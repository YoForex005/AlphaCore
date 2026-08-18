# R020 — Manager API signatures only

**Date:** 2026-08-18  
**Scope:** `GroupTotal`, `GroupNext`, `UserLogins`, `DealRequest`, `ProxySet`  
**Product source:** not modified  
**Secrets:** none (types only; no host/login/password/proxy values)

Sources:

| Surface | Path / assembly | Version |
|---|---|---|
| C++ Manager | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` — `IMTManagerAPI` | `MTManagerAPIVersion 5570` (`30 Jan 2026`) |
| C++ Admin | same header — `IMTAdminAPI` | same |
| C# Manager | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5ManagerAPI64.dll` — `CIMTManagerAPI` | `5.5570.0.0` |
| C# Admin | same DLL — `CIMTAdminAPI` | same |
| C# Web API | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\MT5WebAPI.cs` — `MT5WebAPI` | `WEB_API_VERSION 5570` |

C# native signatures below are reflected from the DLL (no `.cs` sources ship). Web API C# is source.

---

## 1. C++ `IMTManagerAPI`

```cpp
virtual void      ProxySet(const MTProxyInfo &proxy)=0;
virtual uint32_t  GroupTotal(void)=0;
virtual MTAPIRES  GroupNext(const uint32_t pos,IMTConGroup* group)=0;
virtual MTAPIRES  UserLogins(LPCWSTR group,uint64_t*& logins,uint32_t& logins_total)=0;
virtual MTAPIRES  DealRequest(const uint64_t ticket,IMTDeal* deal)=0;
virtual MTAPIRES  DealRequest(const uint64_t login,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
```

Related `DealRequest*` on the same interface:

```cpp
virtual MTAPIRES  DealRequestByGroup(LPCWSTR group,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
virtual MTAPIRES  DealRequestByLogins(const uint64_t *logins,const uint32_t logins_total,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
virtual MTAPIRES  DealRequestByTickets(const uint64_t *tickets,const uint32_t tickets_total,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
virtual MTAPIRES  DealRequestPage(const uint64_t login,const int64_t from,const int64_t to,const uint32_t offset,const uint32_t total,IMTDealArray* deals)=0;
virtual MTAPIRES  DealRequestByLoginsSymbol(const uint64_t *logins,const uint32_t logins_total,LPCWSTR symbol,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
virtual MTAPIRES  DealRequestByGroupSymbol(LPCWSTR group,LPCWSTR symbol,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
```

`ProxySet` argument:

```cpp
#pragma pack(push,1)
struct MTProxyInfo
  {
   enum
     {
      PROXY_SOCKS4   =0,
      PROXY_SOCKS5   =1,
      PROXY_HTTP     =2,
      PROXY_FIRST    =PROXY_SOCKS4,
      PROXY_LAST     =PROXY_HTTP
     };
   int32_t           enable;
   int32_t           type;
   wchar_t           address[64];
   wchar_t           auth[64];
  };
#pragma pack(pop)
```

---

## 2. C++ `IMTAdminAPI`

```cpp
virtual void      ProxySet(const MTProxyInfo &proxy)=0;
virtual uint32_t  GroupTotal(void)=0;
virtual MTAPIRES  GroupNext(const uint32_t pos,IMTConGroup* group)=0;
virtual MTAPIRES  UserLogins(LPCWSTR group,uint64_t*& logins,uint32_t& logins_total)=0;
virtual MTAPIRES  DealRequest(const uint64_t ticket,IMTDeal* deal)=0;
virtual MTAPIRES  DealRequest(const uint64_t login,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
```

Related `DealRequest*` on the same interface:

```cpp
virtual MTAPIRES  DealRequestByGroup(LPCWSTR group,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
virtual MTAPIRES  DealRequestByLogins(const uint64_t *logins,const uint32_t logins_total,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
virtual MTAPIRES  DealRequestByTickets(const uint64_t *tickets,const uint32_t tickets_total,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
virtual MTAPIRES  DealRequestPage(const uint64_t login,const int64_t from,const int64_t to,const uint32_t offset,const uint32_t total,IMTDealArray* deals)=0;
virtual MTAPIRES  DealRequestByLoginsSymbol(const uint64_t *logins,const uint32_t logins_total,LPCWSTR symbol,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
virtual MTAPIRES  DealRequestByGroupSymbol(LPCWSTR group,LPCWSTR symbol,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
```

---

## 3. C# `CIMTManagerAPI` (`MetaQuotes.MT5ManagerAPI`)

```csharp
MTRetCode ProxySet(MTProxyInfo proxy);
UInt32    GroupTotal();
MTRetCode GroupNext(UInt32 pos, CIMTConGroup group);
UInt64[]  UserLogins(String group, out MTRetCode res);
MTRetCode DealRequest(UInt64 ticket, CIMTDeal deal);
MTRetCode DealRequest(UInt64 login, Int64 from, Int64 to, CIMTDealArray deals);
```

Related `DealRequest*` on the same type:

```csharp
MTRetCode DealRequestByGroup(String mask, Int64 from, Int64 to, CIMTDealArray deals);
MTRetCode DealRequestByLogins(UInt64[] logins, Int64 from, Int64 to, CIMTDealArray deals);
MTRetCode DealRequestByTickets(UInt64[] tickets, CIMTDealArray deals);
MTRetCode DealRequestPage(UInt64 login, Int64 from, Int64 to, UInt32 offset, UInt32 total, CIMTDealArray deals);
MTRetCode DealRequestByLoginsSymbol(UInt64[] logins, String symbol, Int64 from, Int64 to, CIMTDealArray deals);
MTRetCode DealRequestByGroupSymbol(String mask, String symbol, Int64 from, Int64 to, CIMTDealArray deals);
```

`ProxySet` argument:

```csharp
struct MTProxyInfo
{
    public enum Type
    {
        PROXY_SOCKS4 = 0,
        PROXY_FIRST  = 0,
        PROXY_SOCKS5 = 1,
        PROXY_HTTP   = 2,
        PROXY_LAST   = 2,
    }
    public Int32  enable;
    public Type   type;
    public String address;
    public String auth;
}
```

`CIMTConGroup`, `CIMTDeal`, `CIMTDealArray`, `MTRetCode` live in `MetaQuotes.MT5CommonAPI`.

---

## 4. C# `CIMTAdminAPI` (`MetaQuotes.MT5ManagerAPI`)

```csharp
MTRetCode ProxySet(MTProxyInfo proxy);
UInt32    GroupTotal();
MTRetCode GroupNext(UInt32 pos, CIMTConGroup group);
UInt64[]  UserLogins(String groups, out MTRetCode res);
MTRetCode DealRequest(UInt64 ticket, CIMTDeal deal);
MTRetCode DealRequest(UInt64 login, Int64 from, Int64 to, CIMTDealArray deals);
```

Related `DealRequest*` match `CIMTManagerAPI` (same parameter types). `UserLogins` parameter name is `groups` on Admin vs `group` on Manager.

---

## 5. C# Web API `MT5WebAPI` (HTTP manager; not native Manager DLL)

Present:

```csharp
MTRetCode GroupTotal(out int total);
MTRetCode GroupNext(uint pos, out MTConGroup conGroup);
MTRetCode UserLogins(string group, out List<ulong> logins);
```

Absent (no members of these names): `ProxySet`, `DealRequest`.

Web-API deal equivalents (not `DealRequest`):

```csharp
MTRetCode DealGet(ulong ticket, out MTDeal deal);
MTRetCode DealGetTotal(ulong login, long from, long to, out uint total);
MTRetCode DealGetPage(ulong login, long from, long to, uint offset, uint total, out List<MTDeal> deals);
```

Protocol helpers (same signatures as the public class):

```csharp
// MTGroupBase
MTRetCode GroupTotal(out int total);
MTRetCode GroupNext(uint pos, out MTConGroup conGroup);

// MTUserBase
MTRetCode UserLogins(string group, out List<ulong> logins);
```

---

## 6. Name map

| Name | C++ Manager/Admin | C# native Manager/Admin | C# Web API |
|---|---|---|---|
| `ProxySet` | `void ProxySet(const MTProxyInfo&)` | `MTRetCode ProxySet(MTProxyInfo)` | — |
| `GroupTotal` | `uint32_t GroupTotal(void)` | `UInt32 GroupTotal()` | `MTRetCode GroupTotal(out int)` |
| `GroupNext` | `MTAPIRES GroupNext(uint32_t, IMTConGroup*)` | `MTRetCode GroupNext(UInt32, CIMTConGroup)` | `MTRetCode GroupNext(uint, out MTConGroup)` |
| `UserLogins` | `MTAPIRES UserLogins(LPCWSTR, uint64_t*&, uint32_t&)` | `UInt64[] UserLogins(String, out MTRetCode)` | `MTRetCode UserLogins(string, out List<ulong>)` |
| `DealRequest` | two overloads (ticket / login+from+to) | two overloads (ticket / login+from+to) | — (`DealGet` / `DealGetPage`) |
