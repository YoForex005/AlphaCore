# W500_SLICE_64

- **slot:** 64
- **file:** `D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h`
- **angle:** Achiever HTTP proxy / `MT_RET_AUTH_MANAGER_IPBLOCK` 1012
- **read:** full file (2083 lines) via `read_file` in three spans (1–1000, 1000–1498, 1490–2083); grep on this file for `MT_RET_AUTH_MANAGER_IPBLOCK|1012|proxy|HTTP|IPBLOCK|Achiever` — **16 hits**, all `MTProxyInfo` / `PROXY_*` / `ProxySet`. **Zero** hits for `MT_RET_AUTH_MANAGER_IPBLOCK`, `1012`, `Achiever`, or `IPBLOCK` in this header. Constant 1012 lives in the included `MT5APIConstants.h` (pulled at line 17).
- **verdict:** PASS

## Binding law (this angle)

Achiever Manager ACL is source-IP based. Local workstation egress is not the allow-list identity; the intended hop is Manager `ProxySet` with `PROXY_HTTP` so the server sees the whitelisted egress. `Connect` must return `MT_RET_AUTH_MANAGER_IPBLOCK` (1012) when the seen IP is outside `IMTConManagerAccess` From/To. 1012 is an **auth-time hard fail**: no Manager session, therefore no dealer/order/deal-write. Callers must not treat 1012 as “connected with empty book.”

This slice judges the **vendor Manager API header** that defines that contract — not the C# / C++ wrappers (those are other files).

## Evidence quotes

Header version pin and the constants include that actually names 1012:

```11:17:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
#define MTManagerAPIVersion  5570
#define MTManagerAPIDate     L"30 Jan 2026"

#include <wincrypt.h>
#include <cstdint>
#include <intrin.h>
#include "MT5APIConstants.h"
```

`EnMTAPIRetcode` in that included file (not this one) is the 1012 definition this compilation unit uses:

```45:47:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIConstants.h
   MT_RET_AUTH_MANAGER_NOCONFIG =1011,    // Manager account doesn't have manager config
   MT_RET_AUTH_MANAGER_IPBLOCK  =1012,    // IP address unallowed for manager
   MT_RET_AUTH_GROUP_INVALID    =1013,    // Group is not initialized (server restart neccesary)
```

HTTP proxy is a first-class Manager type. Address format is `IP:port`. Auth field is `login:password` (format comment only — no secret values in this header). Packed 1-byte alignment.

```73:93:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
//| Proxy description                                                |
//+------------------------------------------------------------------+
#pragma pack(push,1)
struct MTProxyInfo
  {
   //--- types of proxy servers
   enum
     {
      PROXY_SOCKS4   =0,                     // SOCKS4
      PROXY_SOCKS5   =1,                     // SOCKS5
      PROXY_HTTP     =2,                     // HTTP (including NTLM)
      PROXY_FIRST    =PROXY_SOCKS4,          // first type
      PROXY_LAST     =PROXY_HTTP             // last type
     };
   //--- proxy description
   int32_t           enable;                 // proxy enabled or disabled
   int32_t           type;                   // type of proxy server
   wchar_t           address[64];            // IP:port of proxy server
   wchar_t           auth[64];               // login:password
  };
```

`ProxySet` is required **before** `Connect` on both Manager and Admin. `Connect` returns `MTAPIRES` (so 1012 is observable). `ProxySet` is **`void`** — the header cannot report a failed HTTP CONNECT; that surfaces on the subsequent `Connect` as 1012 (wrong/no egress IP) or 7 (`MT_RET_ERR_NETWORK`, included constants).

```161:165:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
   //--- proxy
   virtual void      ProxySet(const MTProxyInfo &proxy)=0;
   //--- connect/disconnect
   virtual MTAPIRES  Connect(LPCWSTR server,uint64_t login,LPCWSTR password,LPCWSTR password_cert,uint64_t pump_mode,uint32_t timeout=INFINITE)=0;
   virtual void      Disconnect(void)=0;
```

```812:815:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
   //--- proxy
   virtual void      ProxySet(const MTProxyInfo &proxy)=0;
   //--- connect/disconnect
   virtual MTAPIRES  Connect(LPCWSTR server,uint64_t login,LPCWSTR password,LPCWSTR password_cert,uint64_t pump_mode,uint32_t timeout=INFINITE)=0;
```

The ACL that **produces** 1012 is first-class on this API. This header includes `Config\MT5APIConfigManager.h` and exposes factory methods for the access-range object:

```22:28:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
#include "Config\MT5APIConfigFirewall.h"
#include "Config\MT5APIConfigGateway.h"
#include "Config\MT5APIConfigGroup.h"
#include "Config\MT5APIConfigHistory.h"
#include "Config\MT5APIConfigHoliday.h"
#include "Config\MT5APIConfigManager.h"
#include "Config\MT5APIConfigNetwork.h"
```

```194:197:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
   virtual IMTConManagerReport* ManagerReportCreate(void)=0;
   virtual IMTConManager* ManagerCreate(void)=0;
   virtual IMTConManagerAccess* ManagerAccessCreate(void)=0;
   virtual MTAPIRES  ManagerCurrent(IMTConManager* manager)=0;
```

`IMTConManagerAccess` is an IP **range** (`From` / `To`), not a single host. If Achiever’s manager login only lists the proxy egress, any other public source IP is 1012.

```49:54:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/Config/MT5APIConfigManager.h
   //--- ip address range from
   virtual LPCWSTR   From(void) const=0;
   virtual MTAPIRES  From(LPCWSTR name)=0;
   //--- ip address range to
   virtual LPCWSTR   To(void) const=0;
   virtual MTAPIRES  To(LPCWSTR value)=0;
```

Admin-side `Firewall*` APIs (lines 862–874) are a **different** control (platform firewall). 1012 is specifically “IP address unallowed for **manager**,” not a closed TCP port. That matches the measured Achiever failure mode: TCP to `:443` can be OPEN while `Connect` still returns 1012.

Post-auth write surfaces exist on `IMTManagerAPI` (they are not reachable on a 1012 session):

```365:369:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
   virtual MTAPIRES  DealerLock(const uint32_t id,IMTRequest* request)=0;
   virtual MTAPIRES  DealerAnswer(IMTConfirm* confirm)=0;
   virtual MTAPIRES  DealerSend(IMTRequest* request,IMTDealerSink* sink,uint32_t& id)=0;
   virtual MTAPIRES  DealerBalance(const uint64_t login,const double value,const uint32_t type,LPCWSTR comment,uint64_t& deal_id)=0;
   virtual MTAPIRES  DealerBalanceRaw(const uint64_t login,const double value,const uint32_t type,LPCWSTR comment,uint64_t& deal_id)=0;
```

```527:529:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
   virtual MTAPIRES  DealAdd(IMTDeal* deal)=0;
   virtual MTAPIRES  DealAddBatch(IMTDealArray* deals,MTAPIRES* results)=0;
   virtual MTAPIRES  DealAddBatchArray(IMTDeal** deals,const uint32_t deals_total,MTAPIRES* results)=0;
```

```553:555:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
   virtual MTAPIRES  OrderAdd(IMTOrder* order)=0;
   virtual MTAPIRES  OrderAddBatch(IMTOrderArray* orders,MTAPIRES* results)=0;
   virtual MTAPIRES  OrderAddBatchArray(IMTOrder** orders,const uint32_t orders_total,MTAPIRES* results)=0;
```

```731:733:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
   virtual MTAPIRES  DealPerform(IMTDeal* deal)=0;
   virtual MTAPIRES  DealPerformBatch(IMTDealArray* deals,MTAPIRES* results)=0;
   virtual MTAPIRES  DealPerformBatchArray(IMTDeal** deals,const uint32_t deals_total,MTAPIRES* results)=0;
```

After a successful connect, `NetworkAddress` can report the seen local/NAT address (useful to confirm the HTTP hop actually changed egress). It is not a substitute for checking `Connect`’s `MTAPIRES`.

```458:463:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
   virtual MTAPIRES  NetworkRescan(const uint32_t flags,const uint32_t timeout)=0;
   virtual uint64_t  NetworkBytesSent(void)=0;
   virtual uint64_t  NetworkBytesRead(void)=0;
   virtual MTAPIRES  NetworkServer(MTAPISTR& server)=0;
   virtual MTAPIRES  NetworkAddress(MTAPISTR& address)=0;
```

This file does **not** contain:

- the token `MT_RET_AUTH_MANAGER_IPBLOCK` or the integer `1012` (those are in `MT5APIConstants.h` / `MT5APIFormat.h`)
- the string `Achiever` or any broker allow-list IP
- any proxy host, port, or `auth` value (only the 64-wchar field layout)
- a silent “connect OK on IP block” path — `Connect` is a typed `MTAPIRES`

Zero-init hazard (caller-side, not a header bug): `MTProxyInfo proxy = {}` leaves `enable=0` and `type=0` (`PROXY_SOCKS4`). If a wrapper forgets `enable=1` + `type=PROXY_HTTP`, the DLL connects **direct** and Achiever returns 1012. That is the documented enable flag working as specified.

`address[64]` / `auth[64]` can truncate a long `host:port` or `user:pass`. Truncation would also present as 7/1012 on `Connect`, still fail-closed.

## Context (not this file; do not treat as this-slice defect)

Product wrappers already map 1012 to the Achiever HTTP-proxy hint and call `ProxySet` with `PROXY_HTTP` before `Connect`:

- `D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs` `Describe`: `1012 => "manager IP blocked — Achiever requires the whitelist HTTP proxy"`
- `D:/Prop/mt5-sdk/src/core/mt5_manager.cpp` `case 1012`: IP blocked / whitelist this machine’s IP

C# `ApplyProxy` treats `ProxySet` as returning `MTRetCode`. This C++ header’s `ProxySet` is `void`. That is a **binding** mismatch in another file, not a missing HTTP type here.

## No-loss implication

`MT_RET_AUTH_MANAGER_IPBLOCK` (1012) is an **authentication** retcode on `Connect`. With this header’s contract, a 1012 result means no Manager/Admin session. Therefore `DealerBalance` / `DealerAnswer` / `DealerSend` / `DealAdd` / `DealPerform` / `OrderAdd` / `TradeAccountSet` cannot run against Achiever. 1012 cannot silently credit, debit, or open a live book.

Missing or disabled HTTP proxy on this LAN is **omission**, not capital loss: ingest/copy stay dark until `ProxySet(PROXY_HTTP)` makes the server see the allow-listed source, or the operator SNAT’s as that IP. Treating 1012 as “empty groups / retry without proxy” is a caller bug; the header still returns a non-`MT_RET_OK` code.

Residual capital risk in *this* file exists only **after** `Connect == MT_RET_OK`: the same interface then exposes dealer and deal-write methods. That is outside the 1012 path. This vendor header does not auto-connect, does not default `enable=1`, and does not place orders by itself.

Not an empty PASS: the assigned file was fully read (2083 lines). HTTP proxy + `Connect` `MTAPIRES` + manager access-range factory are present by construction; 1012 is defined in the included constants unit this header compiles against.
