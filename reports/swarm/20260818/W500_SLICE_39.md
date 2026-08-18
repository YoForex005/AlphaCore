# W500_SLICE_39 — MT5APIManager.h vs dotenv-before-CreateBuilder

| Field | Value |
|---|---|
| Slot | 39 |
| File | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` |
| Angle | env file not loaded before `WebApplication.CreateBuilder` |
| Date | 2026-08-18 |
| Method | `read_file` of the assigned file (full 2083 lines, offsets 1–200 / 200–599 / 598–996 / 997–1395 / 1396–1793 / 1794–2083) + `grep` on that file for `WebApplication`, `CreateBuilder`, `.env`, `dotenv`, `LoadEnv`, `AddEnvironmentVariables`, `appsettings`, `getenv`, `Environment` |
| Product source modified | **No** |
| Verdict | **PASS** (empty PASS — defect class does not apply to this file) |

---

## 1. What was read

`MT5APIManager.h` is the official MetaQuotes Manager/Admin C++ header (vendor, not product host). Banner and version:

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

The file is **2083 lines**. Structure actually present:

1. Includes of other SDK headers (`MT5APIConstants.h`, config/bases `*.h`); `#pragma comment(lib,"advapi32.lib")`.
2. Packed `MTProxyInfo` (enable/type/address/auth fields).
3. `IMTManagerSink` / `IMTDealerSink` notification interfaces.
4. Pure-virtual `IMTManagerAPI` (Connect through GeoResolve; destructor protected at line 780).
5. Pure-virtual `IMTAdminAPI` (Connect, server/firewall/group/manager config, TLS certs, KYC, VPS, etc.; destructor protected at line 1591).
6. DLL entry typedefs / names (`MTManagerVersion`, `MTManagerCreate`, `MTAdminCreate`, `*Ext`).
7. Inline `CMTManagerAPIFactory` — `LoadLibraryW` + `GetProcAddress` of `MT5APIManager64*.dll`, license MD5/RSA verify, AVX CPUID DLL picker.

`grep` on **this file only** for `WebApplication`, `CreateBuilder`, `.env`, `dotenv`, `LoadEnv`, `AddEnvironmentVariables`, `appsettings`, `getenv`, `Environment` → **0 matches**.

There is no `main`, no ASP.NET, no `Microsoft.Extensions.Hosting`, no dotenv helper, no `GetEnvironmentVariable`, no `std::getenv`.

---

## 2. Angle check

The assigned defect is a **.NET host bootstrap** failure: a dotenv / `.env` file must be applied to the process **before** `WebApplication.CreateBuilder(args)` so that `IConfiguration` sees `MT5_*` / FIX / connection-string keys at builder construction.

That pattern exists only in ASP.NET Core hosts. The product host that *does* call CreateBuilder is `D:\Prop\apps\api\Program.cs` line 7:

```
var builder = WebApplication.CreateBuilder(args);
```

That file is **out of scope** for slot 39.

This header cannot be the site of “env not loaded before CreateBuilder” because:

- Language is C++ (`#pragma once`, `class IMTManagerAPI`, `inline MTAPIRES`).
- Hosting model is **Win32 DLL load**, not Kestrel/`WebApplication`.
- Credentials enter as **explicit `Connect` parameters**, not configuration providers:

```164:164:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
   virtual MTAPIRES  Connect(LPCWSTR server,uint64_t login,LPCWSTR password,LPCWSTR password_cert,uint64_t pump_mode,uint32_t timeout=INFINITE)=0;
```

(same signature on `IMTAdminAPI` at line 815.)

- Factory init takes an optional **DLL path**, not an env file:

```1684:1684:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
   MTAPIRES          Initialize(LPCWSTR dll_path=NULL);
```

- `CreateManager`/`CreateAdmin` overloads take `api_version` + optional `datapath` (Manager API working directory), then call `MTManagerCreateExt` / `MTAdminCreateExt`. That `datapath` is not `.env` and is not `WebApplication.CreateBuilder`.
- `SettingGet` / `SettingSet` (lines 496–498, 1216–1218) are **remote Manager server key/value blobs** (`LPCWSTR section, LPCWSTR key`), not process dotenv.
- `FindLibrary` searches `dll_path`, module directory + `/libs`, then PATH for `MT5APIManager64.dll` / `avx` / `avx2` / `arm` — filesystem DLL discovery, not env-file load.

Empty PASS is therefore the measured result for **this file**, not a claim that `apps/api/Program.cs` loads `.env` before CreateBuilder.

---

## 3. Evidence quotes

| Claim | Quote / measurement |
|---|---|
| Vendor C++ Manager API, not .NET host | `#define MTManagerAPIVersion  5570` / `#define MTManagerAPIDate     L"30 Jan 2026"` |
| Connect is explicit args | `virtual MTAPIRES  Connect(LPCWSTR server,uint64_t login,LPCWSTR password,LPCWSTR password_cert,uint64_t pump_mode,uint32_t timeout=INFINITE)=0;` |
| Factory loads a DLL, not dotenv | `if((m_hmodule=::LoadLibraryW(path))==NULL)` then `GetProcAddress(..., s_MTManagerCreateExt)` |
| Default library name | `LPCWSTR library_default=L"MT5APIManager64.dll";` |
| Server settings ≠ process env | `virtual MTAPIRES  SettingGet(LPCWSTR section,LPCWSTR key,LPVOID& outdata,uint32_t& outdata_len)=0;` |
| No host bootstrap in file | `grep` WebApplication / CreateBuilder / dotenv / getenv / Environment / appsettings / `.env` / LoadEnv = **0** |
| File extent actually read | last line 2083: `//+------------------------------------------------------------------+` after `VersionAVX` |

Proxy struct has an `auth` field comment (`login:password`). No live credential values appear in this header; none are quoted here.

---

## 4. No-loss implication

**None on this path for the assigned angle.** A missing `.env` load before `WebApplication.CreateBuilder` cannot originate in `MT5APIManager.h` because the header never starts a .NET host, never reads a dotenv file, and never binds `IConfiguration`. Callers must pass server/login/password into `Connect`; the header does not pull those from the process environment.

This PASS does **not** mean the Manager API is capital-safe in general. `IMTManagerAPI` exposes live trading primitives (`DealerAnswer`, `DealerBalance`, `DealPerform`, `OrderCancel`, `PositionDelete`, `TradeAccountSet`). Those are vendor capabilities for a native caller. They are unrelated to ASP.NET env-file ordering. If a product process later `Connect`s with live Manager credentials, that risk lives in the **caller / host bootstrap** (e.g. `apps/api/Program.cs` or the C++ worker), not in this header’s lack of dotenv.

Capital / no-loss controls (kill-switch, shadow-only, fail-closed missing secrets) are not implemented in this vendor include and cannot be broken by “env after CreateBuilder” inside it.

---

## 5. What this PASS is not

- Not a PASS on `apps/api/Program.cs` dotenv-before-CreateBuilder (not this slot; that file still has `WebApplication.CreateBuilder(args)` at line 7 with no prior env load visible in the first 40 lines).
- Not a claim that live Manager `Connect` is unused elsewhere in the tree.
- Not a claim that `SettingGet`/`datapath` are equivalent to `.env`.
- Not a review of `apps/mt5-worker` or `src/Mt5/Env/EnvFile.cs`.
