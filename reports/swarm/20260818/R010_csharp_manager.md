# R010 — C# Manager API: connect, list groups, users, deals

**Agent:** R010  
**Date:** 2026-08-18  
**Product source modified:** **No** (read-only). Vendor examples and Manager API DLLs were not edited.  
**Passwords copied:** **None.** `SimpleManager.cpp` contains sample `UserAdd` / `UserPasswordCheck` / `UserPasswordChange` string literals; they are **not** reproduced here.

**Asked:** How does C# connect, list groups, users, deals? Read `BalanceExample.NET` and `SimpleManager`. Write method names.

---

## 0. Verdict (measured)

| Question | Answer |
|---|---|
| Is `BalanceExample.NET` C#? | **Yes.** WinForms `.NET Framework 4.7.2` x64/ARM64, refs `MetaQuotes.MT5ManagerAPI64.dll` + `MetaQuotes.MT5CommonAPI64.dll`. |
| Is `SimpleManager` C#? | **No.** Native C++ (`wmain`, `CMTManagerAPIFactory`, `IMTManagerAPI*`). There is **no** `SimpleManager.NET`. |
| Does either sample **list groups**? | **No.** Zero `GroupTotal` / `GroupNext` / `GroupRequest` / `GroupRequestArray` calls. |
| Does either sample **list users**? | **No.** Both hit **one login**: `UserRequest`. SimpleManager also **creates** a user (`UserAdd`). |
| Does either sample **list deals**? | **BalanceExample.NET only**, for one login + time window: `DealRequest(login, from, to, array)`. SimpleManager **writes** deals (`DealerBalance`, `DealerSend`) and never reads history. |
| Where are the C# list methods? | On `CIMTManagerAPI` in `MetaQuotes.MT5ManagerAPI64.dll` (Manager API, version **5570**, date `30 Jan 2026`). Separate HTTP surface: `MT5WebAPI` in `Examples\Web\NET`. |

Honest implication for this product: **do not copy `BalanceExample.NET` or `SimpleManager` as a group/user enumerator.** Use the Manager-API list methods in §4. Cross-ref C++ discovery law: `A39_mt5_group_discovery.md`, `A84_group_total_impl.md`.

---

## 1. Sources (quoted, not modified)

| Path | Role |
|---|---|
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\BalanceExample.NET\Manager.cs` | C# connect / user / deal / dealer-balance wrapper (`CManager`) |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\BalanceExample.NET\BalanceExample.Dialog.cs` | WinForms: Login, GetUser, GetDeals, Deposit, Withdraw |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\BalanceExample.NET\BalanceExample.cs` | `Main` → `CBalanceExampleDlg` |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\BalanceExample.NET\BalanceExample.NET.csproj` | DLL hint paths, net472, x64 |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\SimpleManager\SimpleManager.cpp` | C++ connect + create-user + dealer demo |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\SimpleManager\DealerSink.h` | C++ `IMTDealerSink` (`OnDealerAnswer`) |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\DealerExample.NET\Dealer.cs` | Second C# manager sample (dealer queue, not listing) |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\BalanceExample\Manager.cpp` | C++ twin of `BalanceExample.NET` |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5ManagerAPI64.dll` | C++/CLI wrapper; reflected for exact C# signatures |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5CommonAPI64.dll` | `CIMTUser` / `CIMTDeal` / `CIMTConGroup` / `SMTTime` |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | Native `IMTManagerAPI` / `CMTManagerAPIFactory` |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\MT5WebAPI.cs` | Alternate C# **Web API** (HTTP), not Manager DLL |

SHA-256 (this read):

| SHA-256 | File |
|---|---|
| `04C1901769A5FD30C3AAC6F60B0860F0417D513AFF3E5D7350C3B1EE282A33AA` | `BalanceExample.NET\Manager.cs` (204 lines) |
| `FD8C5230EF71D28DF06D99A9359C128D418547D7956910F2CAA257FB506AAA7C` | `BalanceExample.NET\BalanceExample.Dialog.cs` (309 lines) |
| `802ADA2975B6EA013C049288ECC5930A9307B67F434B93E17C1D7625FF348B99` | `BalanceExample.NET\BalanceExample.cs` (34 lines) |
| `A3D4302D49D31204818A06F8C630964C93B86188F495B6C7A69E6E662530BFDE` | `SimpleManager\SimpleManager.cpp` (266 lines) |
| `128D127261094671C63B36C0118959218F0313F333643AC9533D252D9987C1F4` | `SimpleManager\DealerSink.h` (84 lines) |
| `5E3A4BDEC1C592EF888E6B8483ACA789B72ED5CA613555792DF0BE1C3AC3B8C0` | `DealerExample.NET\Dealer.cs` (537 lines) |
| `41A66C5D65BAE8B114737FB18E330B19A424B1B295BC4FCB5FF9DC251AAAEDAB` | `MetaQuotes.MT5ManagerAPI64.dll` |
| `DB28E45E082B9FAF86169739B5B08FF725C056A974A7A0A4955B649794C0DD2F` | `MetaQuotes.MT5CommonAPI64.dll` |

Reflection used: `[Reflection.Assembly]::LoadFrom` on the two DLLs. `SMTManagerAPIFactory.ManagerAPIVersion == 5570`. Header pin: `#define MTManagerAPIVersion 5570`.

---

## 2. Two C# transports (do not mix names)

| Transport | Namespace / type | How it talks | Used by |
|---|---|---|---|
| **Manager API** (local native) | `MetaQuotes.MT5ManagerAPI.SMTManagerAPIFactory` → `CIMTManagerAPI` | Loads `MT5APIManager64.dll` via C++/CLI | `BalanceExample.NET`, `DealerExample.NET` |
| **Web API** (HTTP JSON) | `MetaQuotes.MT5WebAPI.MT5WebAPI` | TCP + `MTAuth` + command strings (`GROUP_TOTAL`, `USER_USER_LOGINS`, `DEAL_GET_PAGE`) | `Examples\Web\NET` only |

This note’s primary surface is **Manager API**. Web API method names are in §7 because they are the other official C# listing API.

Namespaces on the Manager path:

- `MetaQuotes.MT5ManagerAPI` — factory, `CIMTManagerAPI`, `CIMTAdminAPI`, sinks
- `MetaQuotes.MT5CommonAPI` — `MTRetCode`, `CIMTUser`, `CIMTAccount`, `CIMTDeal`, `CIMTDealArray`, `CIMTConGroup`, `SMTTime`, `EnMTLogCode`

C# lifetime: `IDisposable.Dispose()` on `CIMTManagerAPI` / arrays / users. Native C++ uses `Release()`.

---

## 3. Connect

### 3.1 C# — `BalanceExample.NET` (`CManager`)

Wrapper methods (app-level, not SDK):

| `CManager` method | What it calls |
|---|---|
| `Initialize()` | factory + create manager + allocate user/deal/account objects |
| `Login(string server, UInt64 login, string password)` | `CIMTManagerAPI.Connect` |
| `Logout()` | `CIMTManagerAPI.Disconnect` |
| `Shutdown()` / `Dispose()` | `Dispose` objects + `SMTManagerAPIFactory.Shutdown` |

Factory (static C#; C++ equivalent is instance `CMTManagerAPIFactory`):

| C# (`SMTManagerAPIFactory`) | C++ (`CMTManagerAPIFactory`) |
|---|---|
| `Initialize(string dll_path)` — sample passes `null` | `Initialize(LPCWSTR dll_path=NULL)` |
| `CreateManager(UInt32 version, out MTRetCode res) → CIMTManagerAPI` | `CreateManager(uint32_t version, IMTManagerAPI**)` |
| `CreateManager(UInt32, string datapath, out MTRetCode)` | `CreateManager(version, datapath, **)` |
| `CreateAdmin(...)` | `CreateAdmin(...)` |
| `GetVersion(out UInt32)` | `Version(uint32_t&)` |
| `Shutdown()` | `Shutdown()` |
| `LicenseCheckManager` / `LicenseCheckAdmin` | same names |
| field `ManagerAPIVersion` (`5570`) | `#define MTManagerAPIVersion 5570` |
| field `ManagerAPIDate` (`30 Jan 2026`) | `MTManagerAPIDate` |

`BalanceExample.NET` does **not** call `GetVersion`. `DealerExample.NET` does (`GetVersion` + compare to `ManagerAPIVersion`).

Connect signature (reflected):

```text
MTRetCode CIMTManagerAPI.Connect(
    string server,
    UInt64 login,
    string password,
    string password_cert,          // sample: null
    CIMTManagerAPI.EnPumpModes pump_mode,
    UInt32 timeout)
void CIMTManagerAPI.Disconnect()
MTRetCode CIMTManagerAPI.Subscribe(CIMTManagerSink sink)
```

Native:

```cpp
virtual MTAPIRES Connect(LPCWSTR server, uint64_t login, LPCWSTR password,
                         LPCWSTR password_cert, uint64_t pump_mode,
                         uint32_t timeout=INFINITE)=0;
```

`BalanceExample.NET` `Login`:

```text
m_manager.Connect(server, login, password, null,
                  CIMTManagerAPI.EnPumpModes.PUMP_MODE_FULL,
                  MT5_CONNECT_TIMEOUT)   // 30000 ms
```

Success: `MTRetCode.MT_RET_OK`. UI path: `CBalanceExampleDlg.OnBnClickedLogin` parses `m_Loginname` / `m_Server` / `m_Password` (empty at design time; `App.config` has **no** credentials).

Pre-connect allocations in `Initialize`:

| Call | Type | Purpose |
|---|---|---|
| `DealCreateArray()` | `CIMTDealArray` | reuse buffer for `DealRequest` |
| `UserCreate()` | `CIMTUser` | reuse buffer for `UserRequest` |
| `UserCreateAccount()` | `CIMTAccount` | reuse buffer for `UserAccountRequest` |

Logging used by the sample: `LoggerOut`, `LoggerOutString`.

### 3.2 C# — `DealerExample.NET` (narrower pump)

Same factory. Connect pump is **not** FULL:

```text
Connect(server, login, password, null,
        PUMP_MODE_SYMBOLS | PUMP_MODE_GROUPS | PUMP_MODE_USERS | PUMP_MODE_ORDERS,
        30000)
```

Then `DealerStart()`. Extra subscribe: `Subscribe(this)`, `RequestSubscribe`, `OrderSubscribe`. This sample still does **not** enumerate groups/users/deals.

### 3.3 C++ — `SimpleManager` (not C#)

CLI: `/server:address:port /login:login /password:password` (or interactive prompt). Values are **not** logged here.

Sequence:

| Step | Method |
|---|---|
| 1 | `CMTManagerAPIFactory::Initialize(L"..\\..\\..\\API\\")` — explicit DLL directory (C# sample uses `null`) |
| 2 | `factory.Version(version)` then compare to `MTManagerAPIVersion` |
| 3 | `factory.CreateManager(MTManagerAPIVersion, &manager)` |
| 4 | `manager->Connect(server, login, password, L"", 0, 30000)` — **pump_mode = 0** |
| 5 | optional `NetworkServer(server_name)` + `NetworkRescan(0, 10000)` then `Disconnect` + `Connect` to the better AP |
| 6 | work |
| 7 | `Disconnect` → `Release` → `factory.Shutdown` |

C# names for the same network helpers (on `CIMTManagerAPI`):

| C# | Notes |
|---|---|
| `NetworkRescan(UInt32 flags, UInt32 timeout)` | SimpleManager uses this |
| `NetworkServer(out string server)` | SimpleManager uses this |
| `NetworkAddress(out string address)` | unused by both samples |
| `NetworkBytesSent()` / `NetworkBytesRead()` | unused |

`CBookSink : IMTBookSink` / `OnBook` is compiled into SimpleManager but **never attached** in `wmain`.

### 3.4 Pump flags (`CIMTManagerAPI.EnPumpModes`)

Reflected values (C# adds `PUMP_MODE_NONE = 0`; C++ header has no NONE name):

| Name | Value | Needed to list… |
|---|---:|---|
| `PUMP_MODE_NONE` | 0 | nothing from cache (`SimpleManager` connect) |
| `PUMP_MODE_USERS` | 1 | cache `UserTotal` / `UserGet` / `UserGetByGroup` |
| `PUMP_MODE_ACTIVITY` | 2 | online activity |
| `PUMP_MODE_MAIL` | 4 | mail |
| `PUMP_MODE_ORDERS` | 8 | orders |
| `PUMP_MODE_NEWS` | 32 | news |
| `PUMP_MODE_POSITIONS` | 128 | positions |
| `PUMP_MODE_GROUPS` | 256 | cache `GroupTotal` / `GroupNext` / `GroupGet` |
| `PUMP_MODE_SYMBOLS` | 512 | symbols |
| `PUMP_MODE_HOLIDAYS` | 2048 | holidays |
| `PUMP_MODE_TIME` | 4096 | time config |
| `PUMP_MODE_GATEWAYS` | 8192 | gateways |
| `PUMP_MODE_REQUESTS` | 16384 | dealer requests |
| `PUMP_MODE_PLUGINS` | 32768 | plugins |
| `PUMP_MODE_FULL` | 4294967295 | everything (`BalanceExample.NET`) |

Cache vs request (same as C++ A39):

- `GroupTotal` / `GroupNext` / `GroupGet` / `UserTotal` / `UserGet` / `UserGetByGroup` read the **local pump cache**.
- `GroupRequest` / `GroupRequestArray` / `UserRequest` / `UserRequestArray` / `UserLogins` / `DealRequest*` hit the **server** and do **not** require the matching pump bit.

`BalanceExample.NET` pumps FULL but never walks the group/user cache. `SimpleManager` pumps nothing and only uses request/write APIs.

---

## 4. List groups — **not in either sample**

Discovery methods exist on C# `CIMTManagerAPI`. Neither example calls them.

### 4.1 Cache walk (needs `PUMP_MODE_GROUPS` or `PUMP_MODE_FULL`)

| Method | Signature (C#) | Role |
|---|---|---|
| `GroupCreate` | `CIMTConGroup GroupCreate()` | heap object; caller `Dispose` |
| `GroupCreateArray` | `CIMTConGroupArray GroupCreateArray()` | array for request API |
| `GroupSubscribe` | `MTRetCode GroupSubscribe(CIMTConGroupSink)` | live add/update/delete |
| `GroupUnsubscribe` | `MTRetCode GroupUnsubscribe(CIMTConGroupSink)` | |
| `GroupTotal` | `UInt32 GroupTotal()` | count in cache |
| `GroupNext` | `MTRetCode GroupNext(UInt32 pos, CIMTConGroup group)` | cache item at `pos` |
| `GroupGet` | `MTRetCode GroupGet(string name, CIMTConGroup group)` | cache by name |

Canonical enumerator:

```text
CIMTConGroup g = manager.GroupCreate();
uint n = manager.GroupTotal();
for (uint i = 0; i < n; i++) {
    if (manager.GroupNext(i, g) != MTRetCode.MT_RET_OK) break;
    string name = g.Group();          // hierarchical path, e.g. demo\…
}
g.Dispose();
```

`CIMTConGroup` getters used for discovery (reflected): `Group()`, `Server()`, `Company()`, `Currency()`, plus `Clear()` / `Dispose()`.

`CIMTConGroupArray`: `Total()`, `Next(UInt32)`, `Add(...)`, `Clear()`, `Release()`, `Dispose()`.

### 4.2 Network request (no pump required)

| Method | Signature (C#) | Role |
|---|---|---|
| `GroupRequest` | `MTRetCode GroupRequest(string name, CIMTConGroup group)` | one name from server |
| `GroupRequestArray` | `MTRetCode GroupRequestArray(string mask, CIMTConGroupArray groups)` | mask; `"*"` = all groups this manager may see |

This is the no-pump complete enumerator. Server already applies the manager ACL (`IMTConManager` group masks — **not** the same `GroupTotal`/`GroupNext`).

### 4.3 Config write (not listing)

`GroupUpdate(CIMTConGroup)`, `GroupUpdateBatch(CIMTConGroup[], MTRetCode[])`, `GroupSymbolCreate`, `GroupCommissionCreate`, `GroupTierCreate`.

### 4.4 What the samples actually do with “group”

| Sample | Use |
|---|---|
| `BalanceExample.NET` | **Read** `CIMTUser.Group()` after `UserRequest` and print it in the user-info string. No group config fetch. |
| `SimpleManager` | **Write** `user->Group(L"demo\\demoforex")` before `UserAdd`. Hard-coded group **name**, not an enumeration. |
| `DealerExample.NET` | Passes `m_request.Group()` into `TickLast(symbol, group, out tick)` only. |

---

## 5. List / fetch users

### 5.1 What the samples call

`BalanceExample.NET` `CManager.GetUserInfo` / `GetAccountInfo`:

| Wrapper | SDK call | Fields read |
|---|---|---|
| `GetUserInfo(UInt64 login, out string str)` | `m_user.Clear()` then `UserRequest(login, m_user)` | `Name()`, `Login()`, `Group()`, `Leverage()` |
| `GetAccountInfo(UInt64 login, out string str)` | `m_account.Clear()` then `UserAccountRequest(login, m_account)` | `Balance()`, `Equity()` |

UI: `OnBnClickedGetUser` parses **one** login from `m_User`.

`SimpleManager` (create, not list):

| Method | Role |
|---|---|
| `UserCreate()` | allocate |
| `user->Clear()` / `Name` / `Rights` / `Group` / `Leverage` | fill |
| `UserAdd(user, master_pass, investor_pass)` | create (passwords **not** copied here) |
| `UserUpdate(user)` | rename |
| `UserPasswordCheck(type, login, password)` | check |
| `UserPasswordChange(type, login, password)` | change |
| `UserCertConfirm(login)` | cert |
| `UserRequest(login, user)` | re-fetch created user |
| `user->Release()` | free |

C# names for the same writes (on `CIMTManagerAPI`):

```text
MTRetCode UserAdd(CIMTUser user, string master_pass, string investor_pass)
MTRetCode UserUpdate(CIMTUser user)
MTRetCode UserDelete(UInt64 login)
MTRetCode UserPasswordCheck(CIMTUser.EnUsersPasswords type, UInt64 login, string password)
MTRetCode UserPasswordChange(CIMTUser.EnUsersPasswords type, UInt64 login, string password)
MTRetCode UserCertConfirm(UInt64 login)
```

`CIMTUser` get/set pairs used by samples: `Clear()`, `Login()` / `Login(UInt64)`, `Name()` / `Name(string)`, `Group()` / `Group(string)`, `Leverage()` / `Leverage(UInt32)`, `Rights()` / `Rights(EnUsersRights)`.

### 5.2 C# list methods (unused by both samples)

| Method | Signature (C#) | Cache or request | Meaning |
|---|---|---|---|
| `UserCreate` | `CIMTUser UserCreate()` | — | buffer |
| `UserCreateArray` | `CIMTUserArray UserCreateArray()` | — | buffer |
| `UserSubscribe` / `UserUnsubscribe` | sink | pump users | live |
| `UserTotal` | `UInt32 UserTotal()` | **cache** | count |
| `UserGet` | `MTRetCode UserGet(UInt64 login, CIMTUser)` | **cache** | one |
| `UserRequest` | `MTRetCode UserRequest(UInt64 login, CIMTUser)` | **network** | one (what samples use) |
| `UserGroup` | `MTRetCode UserGroup(UInt64 login, out string group)` | | group name only |
| `UserLogins` | `UInt64[] UserLogins(string group, out MTRetCode res)` | **network** | logins in a group / mask |
| `UserRequestArray` | `MTRetCode UserRequestArray(string group, CIMTUserArray)` | **network** | full user records by group mask |
| `UserRequestByLogins` | `MTRetCode UserRequestByLogins(UInt64[], CIMTUserArray)` | **network** | batch by login list |
| `UserGetByGroup` | `MTRetCode UserGetByGroup(string mask, CIMTUserArray)` | **cache** | |
| `UserGetByLogins` | `MTRetCode UserGetByLogins(UInt64[], CIMTUserArray)` | **cache** | |
| `UserAccountGet` | `MTRetCode UserAccountGet(UInt64, CIMTAccount)` | cache | |
| `UserAccountRequest` | `MTRetCode UserAccountRequest(UInt64, CIMTAccount)` | network | sample account fetch |
| `UserAccountRequestArray` | `MTRetCode UserAccountRequestArray(string group, CIMTAccountArray)` | network | |
| `UserAccountGetByGroup` | `MTRetCode UserAccountGetByGroup(string, CIMTAccountArray)` | cache | |
| `UserAccountGetByLogins` | `MTRetCode UserAccountGetByLogins(UInt64[], CIMTAccountArray)` | cache | |
| `UserAccountRequestByLogins` | `MTRetCode UserAccountRequestByLogins(UInt64[], CIMTAccountArray)` | network | |
| `UserCreateAccount` / `UserCreateAccountArray` | constructors | — | |
| `UserAccountSubscribe` / `UserAccountUnsubscribe` | sink | | live account |
| `UserExternalGet` / `UserExternalRequest` | by external account id | | |

`CIMTUserArray`: `Total()`, `Next(UInt32)`, `Add`, `Clear`, `Release`, `Dispose`.

**How to list users in C# (not in the samples):**

1. List groups (`GroupTotal`/`GroupNext` or `GroupRequestArray("*")`).
2. For each group name, `UInt64[] logins = manager.UserLogins(groupName, out res)` **or** `UserRequestArray(groupName, users)` then `users.Total()` / `users.Next(i)`.
3. Mask `"*"` on `UserLogins` / `UserRequestArray` is the usual “all visible users” request (ACL still applied server-side).

`UserLogins` C# shape differs from C++ (`UserLogins(LPCWSTR, uint64_t*&, uint32_t&)`) — the wrapper returns `UInt64[]` and an `out MTRetCode`.

---

## 6. List / fetch deals

### 6.1 `BalanceExample.NET` — the only listing path

Wrapper: `CManager.GetUserDeal(out CIMTDealArray deals, UInt64 login, DateTime time_from, DateTime time_to)`.

SDK call:

```text
MTRetCode DealRequest(UInt64 login, Int64 from, Int64 to, CIMTDealArray deals)
```

Time conversion: `SMTTime.FromDateTime(DateTime) → Int64`, `SMTTime.ToDateTime(Int64) → DateTime`.

UI `OnBnClickedButtonGetdeals`:

1. `GetUserDeal` for typed login + `m_From` / `m_To`.
2. `for (uint i = 0; i < deal_array.Total(); i++) deal = deal_array.Next(i)`.
3. Keep only `CIMTDeal.EnDealAction` in `{ DEAL_BALANCE, DEAL_CREDIT, DEAL_CHARGE, DEAL_CORRECTION, DEAL_BONUS, DEAL_COMMISSION }`.
4. Display `Time()`, `Deal()`, action name, `Profit()`.

That is **one login’s history**, filtered to balance-style actions. It is **not** “all deals on the server.”

Success retcode for the request: `MT_RET_OK`.

### 6.2 C# `DealRequest*` catalog (reflected)

| Method | Signature (C#) | Meaning |
|---|---|---|
| `DealCreate` | `CIMTDeal DealCreate()` | one deal object |
| `DealCreateArray` | `CIMTDealArray DealCreateArray()` | used by the sample |
| `DealRequest` | `DealRequest(UInt64 ticket, CIMTDeal deal)` | one ticket |
| `DealRequest` | `DealRequest(UInt64 login, Int64 from, Int64 to, CIMTDealArray)` | **sample path** |
| `DealRequestPage` | `DealRequestPage(UInt64 login, Int64 from, Int64 to, UInt32 offset, UInt32 total, CIMTDealArray)` | paged one-login history |
| `DealRequestByGroup` | `DealRequestByGroup(string group, Int64 from, Int64 to, CIMTDealArray)` | all deals in a group window |
| `DealRequestByLogins` | `DealRequestByLogins(UInt64[] logins, Int64 from, Int64 to, CIMTDealArray)` | several logins |
| `DealRequestByTickets` | `DealRequestByTickets(UInt64[] tickets, CIMTDealArray)` | several tickets |
| `DealRequestByGroupSymbol` | `DealRequestByGroupSymbol(string group, string symbol, Int64 from, Int64 to, CIMTDealArray)` | group + symbol |
| `DealRequestByLoginsSymbol` | `DealRequestByLoginsSymbol(UInt64[], string symbol, Int64 from, Int64 to, CIMTDealArray)` | logins + symbol |
| `DealSubscribe` / `DealUnsubscribe` | sink | live deals |
| `DealAdd` / `DealAddBatch` / `DealAddBatchArray` | write | not listing |
| `DealUpdate` / `DealUpdateBatch` / `DealDelete` / `DealDeleteBatch` | write | not listing |
| `DealPerform` / `DealPerformBatch` / `DealPerformBatchArray` | execute stored deal | not listing |

`CIMTDeal` getters used by the sample: `Deal()`, `Action()`, `Profit()`, `Time()`. Also present: `Login()`, `Symbol()`, `Volume()`, `Entry()`, `Reason()`.

`CIMTDealArray`: `Total()`, `Next(UInt32)`, `Add`, `Clear`, `Release`, `Dispose`.

To list **all** deals for a manager: walk groups (§4) then `DealRequestByGroup(group, from, to, array)` (or `UserLogins` + `DealRequestByLogins`). Neither sample does this.

### 6.3 SimpleManager deals — write only

| Method | Role |
|---|---|
| `DealerBalance(login, amount, IMTDeal::DEAL_BALANCE, comment, deal_id)` | deposit (+) / withdrawal (−). Success: `MT_RET_REQUEST_DONE` |
| `RequestCreate()` | build `TA_DEALER_POS_EXECUTE` buy |
| `DealerSend(request, &sink, id)` | send |
| `CDealerSink::Wait` / `OnDealerAnswer` / `OnDealerResult` | wait for `ResultRetcode` |
| `result->ResultDeal()` / `ResultOrder()` / `Print` | print new deal/order |

C# twins (used by `BalanceExample.NET` deposit/withdraw, **not** by SimpleManager):

```text
MTRetCode DealerBalance(UInt64 login, Double amount, UInt32 type, string comment, out UInt64 deal_id)
```

`CManager.DealerBalance(..., bool deposit)` flips the sign. Success: `MT_RET_REQUEST_DONE` (not `MT_RET_OK`).

`DealerExample.NET` extra dealer methods: `DealerStart`, `DealerStop`, `DealerGet`, `DealerLock`, `DealerAnswer`, `DealerSend`, `DealerConfirmCreate`, `DealerUnsubscribe`, `DealerBalanceRaw`.

`CIMTDeal.EnDealAction` values the balance UI exposes: `DEAL_BALANCE=2`, `DEAL_CREDIT=3`, `DEAL_CHARGE=4`, `DEAL_CORRECTION=5`, `DEAL_BONUS=6`, `DEAL_COMMISSION=7`.

---

## 7. Other official C# listing API (`MT5WebAPI`)

Different stack. Method names (do not confuse with Manager DLL):

**Connect**

```text
MTRetCode Connect(string server, int port, ulong login, string password,
                  EnPumpModes pumpModes, EnCryptModes crypt, int timeout)
MTRetCode Connect(string server, int port, ulong login, string password, EnPumpModes pumpModes)
void Disconnect()
```

Internally: `MTConnect.Connect` → `MTAuth.Auth` → `MTAsyncConnect.Start`. This is **not** `CIMTManagerAPI.Connect`.

**Groups** — `MTGroupBase` commands `GROUP_TOTAL` / `GROUP_NEXT`:

```text
MTRetCode GroupTotal(out int total)
MTRetCode GroupNext(uint pos, out MTConGroup conGroup)
MTRetCode GroupGet(string name, out MTConGroup conGroup)
MTRetCode GroupAdd(MTConGroup group, out MTConGroup newConGroup)
MTRetCode GroupDelete(string name)
```

**Users**

```text
MTRetCode UserGet(ulong login, out MTUser user)
MTRetCode UserLogins(string group, out List<ulong> logins)   // WEB_CMD_USER_USER_LOGINS
MTRetCode UserAdd / UserUpdate / UserDelete
MTRetCode UserAccountGet(ulong login, out MTAccount account)
```

**Deals** — names are `DealGet*`, not `DealRequest*`:

```text
MTRetCode DealGet(ulong ticket, out MTDeal deal)                          // DEAL_GET
MTRetCode DealGetTotal(ulong login, long from, long to, out uint total)   // DEAL_GET_TOTAL
MTRetCode DealGetPage(ulong login, long from, long to, uint offset, uint total, out List<MTDeal> deals)  // DEAL_GET_PAGE
```

---

## 8. Method-name cheat sheet

### Used by `BalanceExample.NET` (C#)

`SMTManagerAPIFactory.Initialize` · `CreateManager` · `Shutdown`  
`CIMTManagerAPI.Connect` · `Disconnect` · `Dispose`  
`DealCreateArray` · `UserCreate` · `UserCreateAccount`  
`UserRequest` · `UserAccountRequest`  
`DealRequest(login, from, to, array)`  
`DealerBalance`  
`LoggerOut` · `LoggerOutString`  
`CIMTUser.Clear/Name/Login/Group/Leverage`  
`CIMTAccount.Clear/Balance/Equity`  
`CIMTDealArray.Total/Next`  
`CIMTDeal.Action/Deal/Profit/Time`  
`SMTTime.FromDateTime` · `ToDateTime`

App wrappers: `CManager.Initialize/Login/Logout/Shutdown/Dispose/GetUserInfo/GetAccountInfo/GetUserDeal/DealerBalance`.

### Used by `SimpleManager` (C++, for comparison)

`CMTManagerAPIFactory.Initialize/Version/CreateManager/Shutdown`  
`IMTManagerAPI.Connect/Disconnect/Release`  
`NetworkServer` · `NetworkRescan` · `Free` · `CustomCommand`  
`UserCreate` · `UserAdd` · `UserUpdate` · `UserPasswordCheck` · `UserPasswordChange` · `UserCertConfirm` · `UserRequest`  
`RequestCreate` · `DealerBalance` · `DealerSend`  
`MailCreate` · `MailSend`  
`CDealerSink.Initialize/Wait/OnDealerAnswer/OnDealerResult`

### Must call to **list** (C# Manager API; unused by both samples)

| List | Methods |
|---|---|
| Groups | `GroupCreate` + `GroupTotal` + `GroupNext` **or** `GroupCreateArray` + `GroupRequestArray("*")` |
| Users | `UserLogins(group, out res)` **or** `UserCreateArray` + `UserRequestArray(group)` **or** cache `UserTotal`/`UserGet` after `PUMP_MODE_USERS` |
| Deals | `DealRequestByGroup` / `DealRequestByLogins` / `DealRequestPage` (sample only does single-login `DealRequest`) |

---

## 9. C# vs C++ wrapper deltas (do not copy C++ signatures blindly)

| Topic | C# | C++ |
|---|---|---|
| Factory | static `SMTManagerAPIFactory` | instance `CMTManagerAPIFactory` |
| Create | returns `CIMTManagerAPI`, `out MTRetCode` | `MTAPIRES` + out pointer |
| Destroy | `Dispose()` | `Release()` |
| Pump arg type | `EnPumpModes` | `uint64_t` |
| `UserLogins` | `UInt64[]` + `out MTRetCode` | `uint64_t*&` + `uint32_t&` |
| `GroupRequestArray` mask | `string` | `LPCWSTR` |
| Times | `SMTTime.FromDateTime` / `ToDateTime` | `SMTTime::STToTime` / Unix `int64_t` |
| `BalanceExample` connect pump | `PUMP_MODE_FULL` | C++ twin `BalanceExample` uses pump `0` |
| SimpleManager DLL path | n/a | `..\..\..\API\` |

---

## 10. Security / product notes

- No live broker host, login, or password appears in `BalanceExample.NET` `App.config` or Designer defaults. UI fields are empty until the operator types them.
- `SimpleManager` embeds **sample** master/investor/check/change password literals in `UserAdd` / `UserPasswordCheck` / `UserPasswordChange`. **Not copied in this report.** Do not treat them as lab credentials.
- Product C# under `D:\Prop\src` was not modified. This file is documentation of vendor examples + reflected official method names.

---

## 11. Honesty box

| Claim | Status |
|---|---|
| “C# SimpleManager lists groups/users/deals” | **False.** SimpleManager is C++ and does not list. |
| “BalanceExample.NET lists groups” | **False.** |
| “BalanceExample.NET lists all users” | **False.** One `UserRequest(login)`. |
| “BalanceExample.NET lists deals” | **Partial.** One login + time range + balance-action filter. |
| “C# Manager API can list groups/users/deals” | **True.** Methods in §4–§6, confirmed by reflection of `MetaQuotes.MT5ManagerAPI64.dll` 5570. |
| Live connect against a trade server exercised here | **No.** Source + DLL reflection only. |
