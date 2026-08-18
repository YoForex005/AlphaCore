# W500_SLICE_89 — MT5APIManager.h vs env-before-CreateBuilder

| Field | Value |
|---|---|
| Slot | 89 |
| File | `D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h` |
| Angle | env file not loaded before `WebApplication.CreateBuilder` |
| Date | 2026-08-18 |
| Method | Full `read_file` of the assigned header (lines 1–2083) + `grep` on that file and `Include/` for `WebApplication` / `CreateBuilder` / dotenv / `AddEnvironmentVariables` / `.env` / `IConfiguration` |
| Product source modified | **No** |
| Verdict | **PASS** (empty PASS — defect class does not apply to this file) |

---

## 1. What was read

`MT5APIManager.h` is the official MetaQuotes C++ Manager / Admin API header (version pin `MTManagerAPIVersion 5570`, date `30 Jan 2026`). It is not an ASP.NET host, not a .NET `Program.cs`, and not a dotenv loader.

Structure after a full-file read:

1. **Lines 1–67** — copyright, `#pragma once`, version macros, native includes (`wincrypt.h`, `cstdint`, `intrin.h`) plus sibling `Config\` / `Bases\` headers.
2. **Lines 76–92** — `MTProxyInfo` (proxy type + address + auth buffer). No process-env read.
3. **Lines 97–781** — `IMTManagerAPI` pure-virtual Manager surface (`Connect`, groups, symbols, users, deals, orders, positions, ticks, dealer, settings, ECN, clients, …).
4. **Lines 785–1592** — `IMTAdminAPI` pure-virtual Admin surface (same native COM-style API, extra server/firewall/route/gateway/feeder config).
5. **Lines 1594–2082** — `CMTManagerAPIFactory`: `LoadLibraryW` / `GetProcAddress` of `MT5APIManager64*.dll`, `CreateManager` / `CreateAdmin`, CryptoAPI license sign check, CPUID/AVX DLL pick.

`grep` on this file for `WebApplication|CreateBuilder|AddEnvironmentVariables|\.env|DotNetEnv|LoadEnv|IConfiguration|UseEnvironment` → **0 hits**.

`grep` on `D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include` for `WebApplication|CreateBuilder|DotNetEnv|AddJsonFile|appsettings|Host.Create` → **0 hits**.

---

## 2. Angle check

The assigned defect is a **.NET host bootstrap** failure: a dotenv / `.env` file must be applied to the process **before** `WebApplication.CreateBuilder(args)` so that `IConfiguration` sees `MT5_*` / FIX / connection-string keys.

That pattern exists only in ASP.NET Core hosts. This file cannot exhibit it:

- Language is C++ (`#pragma once`, `class IMTManagerAPI`, `inline CMTManagerAPIFactory::Initialize`).
- No `Microsoft.AspNetCore` types, no `WebApplication`, no `CreateBuilder`, no `Host.CreateApplicationBuilder`.
- No `DotNetEnv`, `AddEnvironmentVariables`, `AddJsonFile`, `appsettings`, `IConfiguration`, or `.env` parse.
- Credentials are **caller-supplied `LPCWSTR` arguments**, not environment lookups.
- The only “bootstrap” is native DLL load by filename (`MT5APIManager64.dll` / avx / avx2 / arm), not configuration-provider composition.

Empty PASS is the measured result for **this file**, not a claim that the product API host loads `.env` before `CreateBuilder`.

---

## 3. Evidence quotes

Vendor identity and version (C++ Manager API, not a web host):

```1:12:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
//+------------------------------------------------------------------+
//|                                         MetaTrader 5 API Manager |
//|                             Copyright 2000-2026, MetaQuotes Ltd. |
//|                                               www.metaquotes.net |
//+------------------------------------------------------------------+
#pragma once

//+------------------------------------------------------------------+
//| Manager API version                                              |
//+------------------------------------------------------------------+
#define MTManagerAPIVersion  5570
#define MTManagerAPIDate     L"30 Jan 2026"
```

Connect takes explicit server/login/password pointers. No env-file or configuration-provider call:

```163:165:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
   //--- connect/disconnect
   virtual MTAPIRES  Connect(LPCWSTR server,uint64_t login,LPCWSTR password,LPCWSTR password_cert,uint64_t pump_mode,uint32_t timeout=INFINITE)=0;
   virtual void      Disconnect(void)=0;
```

`SettingGet` / `SettingSet` are Manager-server key/value blobs (`LPCWSTR section, LPCWSTR key`), not dotenv / `IConfiguration`:

```495:498:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
   //--- settings
   virtual MTAPIRES  SettingGet(LPCWSTR section,LPCWSTR key,LPVOID& outdata,uint32_t& outdata_len)=0;
   virtual MTAPIRES  SettingSet(LPCWSTR section,LPCWSTR key,const LPVOID indata,const uint32_t indata_len)=0;
   virtual MTAPIRES  SettingDelete(LPCWSTR section,LPCWSTR key)=0;
```

Factory bootstrap is `LoadLibraryW` + named exports. No `CreateBuilder`, no `.env`:

```1719:1744:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
inline MTAPIRES CMTManagerAPIFactory::Initialize(LPCWSTR dll_path/*=NULL*/)
  {
   wchar_t  path[MAX_PATH]={};
//--- find the Manager API DLL
   if(!FindLibrary(dll_path,path,_countof(path)-1))
      return(MT_RET_ERR_NOTFOUND);
//--- load Manager API DLL
   if((m_hmodule=::LoadLibraryW(path))==NULL)
      return(MT_RET_ERR_NOTFOUND);
//--- find entry point addresses
   m_mtversion=reinterpret_cast<MTManagerVersion_t>(::GetProcAddress(m_hmodule,s_MTManagerVersion));
//--- find for manager
   if((m_mtmanager_ext=reinterpret_cast<MTManagerCreateExt_t>(::GetProcAddress(m_hmodule,s_MTManagerCreateExt)))==NULL)
      m_mtmanager=reinterpret_cast<MTManagerCreate_t>(::GetProcAddress(m_hmodule,s_MTManagerCreate));
   // ...
   return(MT_RET_OK);
  }
```

DLL search is filesystem (`dll_path`, module folder, `libs\`, then PATH name). Not environment-variable key material:

```1769:1831:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
inline bool CMTManagerAPIFactory::FindLibrary(LPCWSTR dll_path,wchar_t *path,const size_t path_maxlen)
  {
   LPCWSTR library_default=L"MT5APIManager64.dll";
   LPCWSTR library=library_default;
   // ...
   if(dll_path)
     {
      _snwprintf_s(path,path_maxlen,_TRUNCATE,L"%s\\%s",dll_path,library);
      if(GetFileAttributesW(path)!=INVALID_FILE_ATTRIBUTES)
         return(true);
      // ...
     }
   ::GetModuleFileNameW(NULL,folder,_countof(folder)-1);
   // 3 parent levels + \libs\
   wcsncpy_s(path,path_maxlen,library,_TRUNCATE);
   return(true);
  }
```

| Claim | Quote / measurement |
|---|---|
| Native C++ header | `#pragma once` + `#define MTManagerAPIVersion  5570` |
| No ASP.NET host APIs | `grep` `WebApplication` / `CreateBuilder` / dotenv / `IConfiguration` = **0** |
| Creds are call arguments | `Connect(LPCWSTR server,uint64_t login,LPCWSTR password,LPCWSTR password_cert,...)` |
| Factory loads a DLL | `LoadLibraryW(path)` + `GetProcAddress(...,"MTManagerCreateExt")` |
| File ends at factory AVX helper | line 2082 `}` then line 2083 `//+------------------------------------------------------------------+` |

---

## 4. No-loss implication

**None on this path.** A missing `.env` load before `WebApplication.CreateBuilder` cannot originate in `MT5APIManager.h` because the header never starts a .NET host, never reads process environment, and never binds `MT5_*` / FIX / SQL keys. Callers must pass Manager credentials as `LPCWSTR` into `Connect`. Worst case inside this header is `MT_RET_ERR_NOTFOUND` / `MT_RET_ERROR` when the native DLL is missing or `Connect` is invoked with empty caller-supplied strings — that is fail-closed native load, not silent live trading on default secrets. This file does not send customer orders, does not size risk, and does not substitute dummy books when config is absent.

---

## 5. What this PASS is not

- Not a PASS on `apps/api/Program.cs` (or any other product host) dotenv-before-CreateBuilder — **out of this slot**.
- Not a claim that live Manager `Connect` is wired with real broker credentials in the C# layer.
- Not a review of `NativeMt5BrokerConnector` / `LiveMt5Registration` env binding (those files consume this API; they are not this file).
- Not a claim the vendor `SettingGet` surface is a dotenv replacement.

Empty-PASS justification: the assigned file was fully read (2083 lines). The env-before-CreateBuilder defect class is absent by language and API shape, not by skipped review.
