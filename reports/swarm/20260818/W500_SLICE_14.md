# W500_SLICE_14

- **slot:** 14
- **file:** `D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h`
- **angle:** Achiever HTTP proxy / `MT_RET_AUTH_MANAGER_IPBLOCK` 1012
- **read:** header via `read_file` in two spans (L1–1000 and L1000–1645+); grep on this file for `MT_RET_AUTH_MANAGER_IPBLOCK|1012|proxy|Proxy|HTTP|Achiever|IPBLOCK`; cross-checked included `MT5APIConstants.h` L46 and `mt5_manager.cpp` 1012 mapper. File is official MetaQuotes Manager API (version **5570**, date `30 Jan 2026`).
- **verdict:** PASS
- **secrets printed:** none (no proxy `auth`, manager password, or FIX password values)

This is **not** an empty PASS. The assigned file was read. The angle is present as official `MTProxyInfo::PROXY_HTTP` + `ProxySet` (pre-`Connect` hop). Retcode **1012** is not spelled in this header body; it is in the header’s first include, `MT5APIConstants.h`.

---

## 0. Ruling

**PASS.** `MT5APIManager.h` is the official Manager/Admin surface Achiever local connect must use. It exposes HTTP proxy as a first-class, pre-connect setting (`PROXY_HTTP = 2`, `IMTManagerAPI::ProxySet` / `IMTAdminAPI::ProxySet`). A source IP that is not on the manager access list fails authentication with **1012** (`MT_RET_AUTH_MANAGER_IPBLOCK`, included constants). That is fail-closed: no session, therefore none of this header’s later dealer/deal/order mutators can run.

Achiever is not named in this vendor header (expected: generic MetaQuotes). The Prop binding is external: architecture / `docs/deployment.md` require Achiever egress `81.29.145.69`; a desktop that is not that IP must `ProxySet` HTTP so the broker sees the allow-listed hop. R012 already measured that local connect without that hop is 1012. This slice only judges the official header contract.

| Check | Result |
|---|---|
| HTTP proxy type in this file? | **Yes.** `PROXY_HTTP = 2` (“HTTP (including NTLM)”) |
| Apply-before-connect API? | **Yes.** `virtual void ProxySet(const MTProxyInfo &proxy)=0` on manager **and** admin |
| 1012 / `MT_RET_AUTH_MANAGER_IPBLOCK` in this file body? | **No** (not a defect). Defined in included `MT5APIConstants.h` L46 |
| 1012 reachable as `MTAPIRES` from `Connect`? | **Yes.** `Connect(...)` returns `MTAPIRES`; constants enum is in scope via `#include "MT5APIConstants.h"` |
| Achiever-specific strings? | **None** (vendor-generic) |
| Live capital send in the proxy/1012 path? | **No.** Proxy + Connect are session setup. 1012 rejects before any deal/order API. |

---

## Evidence quotes

Include chain (1012 is in-scope when this header is compiled):

```14:18:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
#include <intrin.h>
#include "MT5APIConstants.h"
#include "MT5APILogger.h"
#include "MT5APIPublicKey.h"
#include "MT5APITools.h"
```

HTTP proxy descriptor (the Achiever hop type). `auth[64]` is documented as `login:password`; **values are not quoted here**.

```73:92:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
//+------------------------------------------------------------------+
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

Manager apply + connect order (proxy is **not** an argument of `Connect`; caller must `ProxySet` first):

```161:164:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
   //--- proxy
   virtual void      ProxySet(const MTProxyInfo &proxy)=0;
   //--- connect/disconnect
   virtual MTAPIRES  Connect(LPCWSTR server,uint64_t login,LPCWSTR password,LPCWSTR password_cert,uint64_t pump_mode,uint32_t timeout=INFINITE)=0;
```

Admin API has the same pair (duplicate surface, same contract):

```812:815:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
   //--- proxy
   virtual void      ProxySet(const MTProxyInfo &proxy)=0;
   //--- connect/disconnect
   virtual MTAPIRES  Connect(LPCWSTR server,uint64_t login,LPCWSTR password,LPCWSTR password_cert,uint64_t pump_mode,uint32_t timeout=INFINITE)=0;
```

Manager access factory on this API (IP allow-list is **server-side config**, not a client constant). 1012 is the client-visible refusal when the TCP source is outside `IMTConManagerAccess` From/To ranges:

```194:197:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
   virtual IMTConManagerReport* ManagerReportCreate(void)=0;
   virtual IMTConManager* ManagerCreate(void)=0;
   virtual IMTConManagerAccess* ManagerAccessCreate(void)=0;
   virtual MTAPIRES  ManagerCurrent(IMTConManager* manager)=0;
```

Included retcode (not in `MT5APIManager.h` body; required for this angle):

```45:47:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIConstants.h
   MT_RET_AUTH_MANAGER_NOCONFIG =1011,    // Manager account doesn't have manager config
   MT_RET_AUTH_MANAGER_IPBLOCK  =1012,    // IP address unallowed for manager
   MT_RET_AUTH_GROUP_INVALID    =1013,    // Group is not initialized (server restart neccesary)
```

Prop SDK maps that same 1012 (implementation, not this vendor file). Quote only the mapper; no credentials:

```60:68:D:/Prop/mt5-sdk/src/core/mt5_manager.cpp
// Maps MT5 API error codes to human-readable disconnect reasons
static std::string mt5ErrorReason(MTAPIRES code) {
    switch (code) {
        case 7:    return "Network timeout (MT_RET_ERR_NETWORK). MT5 server unreachable - check proxy/firewall and MT5 server IP whitelist.";
        case 1012: return "IP blocked by MT5 server (MT_RET_AUTH_MANAGER_IPBLOCK). Ask MT5 server admin to whitelist this machine's IP.";
        case 5:    return "No connection to MT5 server (MT_RET_ERR_NOCONNECT). Server may be offline or wrong address configured.";
        case 3:    return "Wrong credentials (MT_RET_AUTH_MANAGER_FAILED). Check MT5 manager login/password in config.";
        default:   return "Connection failed with MT5 error code " + std::to_string((int)code) + ".";
    }
}
```

`SetProxy` / `Connect` in `mt5_manager.cpp` fill `MTProxyInfo`, call `ProxySet`, then `Connect`. That is the correct use of this header. Proxy credentials are formatted into `auth[]` in that .cpp; **not reproduced**.

Grep on the assigned file: **13** hits — all `MTProxyInfo` / `PROXY_*` / `ProxySet`. **Zero** hits for `Achiever`, `MT_RET_AUTH_MANAGER_IPBLOCK`, or the literal `1012`. Nearby vendor `1012` tokens (`CONDITION_ACCOUNT_LEVERAGE`, `ACTION_ACCOUNT_ARCHIVE`, dataset field ids) are **different enums** and are not this retcode.

This header also declares post-auth mutators (`DealerBalance`, `DealAdd`, `OrderAdd`, `DealPerform`, …). Those are **out of this slice’s angle**. They are gated by a successful `Connect`. 1012 means they never run.

---

## No-loss implication

`MT_RET_AUTH_MANAGER_IPBLOCK` (1012) is an **authentication** retcode: “IP address unallowed for manager.” The broker rejects the Manager TCP identity **before** a session exists. No pump, no `DealRequest`, no `DealerAnswer`, no `DealerBalance`, no `OrderAdd`. Equity cannot move on a connection that never authenticated.

Achiever HTTP proxy, in this header, is only `MTProxyInfo` + `ProxySet`. It changes the **source IP the server sees**. It does not place orders, size positions, or confirm dealer requests. On a workstation whose public NAT is not `81.29.145.69`, omitting `PROXY_HTTP` is expected to yield 1012 (R012). That failure is **no-loss**: live Achiever books stay untouched because there is no Manager session.

If proxy **is** applied and Connect succeeds, capital-risk APIs in this same header become reachable — that is a later-slot / live-wiring concern, not 1012. C# product still does not load this DLL for live Achiever (C42 / Fake connector). Slot 14 therefore has **no live capital-loss path on the proxy / 1012 contract**.

**Honest limit:** this vendor file cannot *prove* a live Achiever session or that the HTTP hop is currently enabled on this desktop. It only proves the official API exists and that 1012 is fail-closed.

---

## Scope notes (honesty)

- Product source was **not** modified.
- Proxy host/port/user/password values were **not** copied into this report.
- Empty-PASS rule does not apply: HTTP proxy evidence is in the assigned file at L73–92 and L161–164 / L812–813.
