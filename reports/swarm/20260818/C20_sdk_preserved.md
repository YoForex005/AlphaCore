# C20 — mt5-sdk C++ not deleted or rewritten

| Field | Value |
|---|---|
| Agent | C20 (senior engineer, preservation verify only) |
| Date | 2026-08-18 13:25:35 +05:30 |
| Assigned | Confirm `mt5-sdk` C++ is not deleted or rewritten. Write this report. Do not modify product source. |
| Tree | `D:\Prop\mt5-sdk` |
| Product source modified | **No.** This report is the only write. |
| Nested HEAD | `a8f3fe85bc0adf109acb5ec72ed8adb2c0a289df` |
| Parent gitlink | mode `160000` → same SHA (`git ls-files -s -- mt5-sdk`) |
| Parent HEAD (unrelated) | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback…`) |
| Nested working tree | **clean** vs HEAD (no staged, unstaged, untracked, or deleted tracked files) |
| Nested commits on `main` | **1** (extraction only; no rewrite commit) |

---

## 0. Verdict

**PRESERVED. Not deleted. Not rewritten.**

`D:\Prop\mt5-sdk` is on disk as a **nested C++20 CMake library** (gitlink, not a vanished folder). The working tree is **byte-identical** to the single extraction commit `a8f3fe85` (“Extract reusable MT5 SDK from the YoPips prop-firm backend”, 2026-07-31). Every first-party `src/` / `config/` / `tests/` blob matches HEAD. The vendored MetaQuotes Manager API (`vendor/MetaTrader5SDK/`) is still present (80 headers, 12 DLLs, 895 example files, 3 CHM docs).

The C# project `D:\Prop\src\Mt5` is a **separate** net8 collector with `FakeMt5BrokerConnector`. It does **not** replace, delete, or reimplement the C++ tree. No `DllImport` / `NativeLibrary` / `MT5APIManager` usage exists under `D:\Prop\src`.

| Claim | Measured |
|---|---|
| SDK directory deleted | **False.** Tree exists. |
| First-party C++ deleted | **False.** 17 `.cpp` + 16 `.h` under `src/`, `config/`, `tests/`. |
| SDK rewritten in this Prop wave | **False.** Nested repo has one commit; WT `git diff HEAD` is empty; all first-party `git hash-object` blobs **MATCH** `git ls-files`. |
| C# rewrite of Manager API | **False.** `TraderIntelligence.Mt5.csproj` has no native refs; connector is in-memory fake. |
| Vendor MetaQuotes SDK stripped | **False.** `Include/MT5APIManager.h` (133 640 B) and `Libs/MT5APIManager64.dll` (7 185 272 B) present. |
| Parent gitlink detached / dirty | **False.** Parent `160000 a8f3fe85… 0 mt5-sdk` equals nested `HEAD`. `git status --porcelain -- mt5-sdk` empty. |

Classification (same as `D:\Prop\reports\PHASE0_AUDIT.md`): **`EXISTS_AND_GOOD`** as the C++ transport. It is **not** the C# Phase-1 collector.

---

## 1. What “not rewritten” means here

Two independent facts were measured:

1. **Not deleted** — path `D:\Prop\mt5-sdk` exists; CMake still declares `project(mt5sdk LANGUAGES CXX)` / `CMAKE_CXX_STANDARD 20`; `IMT5Client`, `MT5Manager`, `MT5HttpClient` files are present and non-empty.
2. **Not rewritten** — the nested repository was not force-pushed, rebased, or edited after extraction. There is no second commit. There is no working-tree delta. Line counts of the load-bearing files equal the `a8f3fe85` `--stat` numbers.

This agent did **not** rebuild CMake, run tests, or edit any file under `D:\Prop\mt5-sdk` or `D:\Prop\src`.

---

## 2. Git identity (binding)

### 2.1 Nested repo

```
repo:   D:\Prop\mt5-sdk
branch: main → origin/main
remote: https://github.com/YoForex005/mt5-sdk.git
HEAD:   a8f3fe85bc0adf109acb5ec72ed8adb2c0a289df
author: mql5helpline <mql5helpline@gmail.com>
date:   2026-07-31T01:13:45-07:00
subject: Extract reusable MT5 SDK from the YoPips prop-firm backend
tracked files: 1027
deleted tracked: (none)
untracked: (none)
diff vs HEAD: (empty)
```

That commit introduced **1027 files / 223 819 insertions** and is still the tip.

### 2.2 Parent `D:\Prop` records a gitlink, not a copy

```
160000 a8f3fe85bc0adf109acb5ec72ed8adb2c0a289df 0	mt5-sdk
```

No `.gitmodules` (already noted in A103). The directory is a **separate git repo** pinned by SHA. Parent porcelain for `mt5-sdk` is empty — the pin has not been moved.

---

## 3. First-party C++ inventory (still the extraction set)

README “What's in here” vs disk — **17/17 present**. CMake `MT5SDK_SOURCES` + optional + test + probe lists — **all paths exist**.

| Rel path | Bytes | Physical lines | SHA-256 | HEAD blob (full) | vs HEAD |
|---|---:|---:|---|---|---|
| `CMakeLists.txt` | 6206 | 173 | `15298345532CA0D33888E919D14F680B933EB60C6C2A2CE85DBBF1F0D05419719E9` | `31278758d322b95be6f471b2f8663055eeb21b4e` | MATCH |
| `README.md` | 6843 | — | `18E62708EB0DA53E483579A78CECE5B7A981BFD1B05CE91D22A81487538A59D5C` | `87266d106256…` | MATCH |
| `config/app_config.cpp` | 6370 | — | `12304425FC42E61563754CF8ED40786E52977A1EF4F975450B1CB6E764FC6BE` | `6fb9117aac36…` | MATCH |
| `config/app_config.h` | 2824 | — | `562EE8B969C4B069A340053D6F5A868D1E7E38769F6A8A2AD74C80626D1FF38B83` | `fb82c7293ae1…` | MATCH |
| `src/core/imt5_client.h` | 9625 | **176** | `153CB8D632BB94ADC1145C0343418788010E6FEDC6886979A59B34E6332B104C707` | `c3d96b42bcd5e0407642bd73916d6df269a1320d` | MATCH |
| `src/core/mt5_types.h` | 25328 | **571** | `5361D3BE309AC89141C82EFD8F775812913412B5AA293C9B300D948B65329A99C63` | `bbbb080063c5e538823f588ad115af92c861192c` | MATCH |
| `src/core/mt5_manager.h` | 10363 | **207** | `1740C098926BDFD966B91231148EE91FC371F841C84224B223712C3C8EDAB277B79` | `513ed62bd665…` | MATCH |
| `src/core/mt5_manager.cpp` | 62958 | **1558** | `C25AD8CA9ACFBC5B64AB101C5BCDFCD1CF3CA6FE362BFCD2FC84EDC2EA2AFA98` | `43594571cabfbb44c26513b327bde9535ee99935` | MATCH |
| `src/core/mt5_http_client.h` | 8644 | — | `17738DBC7EE7E0C8EA637652272C6252626ED43E6B33AD8C39A7521F5BF04E98588` | `7d565cd457c2…` | MATCH |
| `src/core/mt5_http_client.cpp` | 34509 | **831** | `185D4ED9AAC6D9662B0765507CD8429CAA6A56CB640CE74715E3F237AB2FF83AF6` | `31a121a121c8…` | MATCH |
| `src/core/mt5_pool.h` | 6729 | — | `1396A4012B8A394C978DD3965280C6C659F8A007F3684D11A5478BA40DD6879D539` | `21ba9834b1ce…` | MATCH |
| `src/core/mt5_pool.cpp` | 44877 | **1097** | `923B37A418B8A0498EC444A2D577FE1DCE78ED3D7E77616F2AA97BC8264AFCA9D69` | `071c68a2ec45…` | MATCH |
| `src/core/mt5_watchdog.h` | 1218 | — | `38C1C3A5A7F12B16C1656D3647D5EC8CBCD1015D4BD22DB802897C0D08404D3D03` | `72c5ba2fa3f4…` | MATCH |
| `src/core/mt5_watchdog.cpp` | 3003 | **86** | `73AFDC003ABB07CAACDCAFB0E5793FE8249E06AC92ADAADDCF8F5EB5A810EA09C1` | `b1a184b752ef…` | MATCH |
| `src/core/mt5_tick_bridge.h` | 10553 | — | `179B759D636D8F51D24FA15CA1BDA6A65D2E98958CE73193E53AF5ACBC337C91E68` | `b7bf5ae859ea…` | MATCH |
| `src/core/mt5_tick_bridge.cpp` | 14271 | — | `330F18FB606AE465921D3F80A6507A8615F4FF820EDA048ACDD16CDF042666D5720` | `067d555922c8…` | MATCH |
| `src/core/chart_timeframe.{h,cpp}` | 694 / 2923 | — | (hashed; MATCH) | MATCH | MATCH |
| `src/db/pg_pool.{h,cpp}` | present | — | MATCH | MATCH | MATCH |
| `src/services/metrics_service.h` | 31480 | — | MATCH | MATCH | MATCH |
| `src/services/mt5_{account_helper,ledger_store,time_window}.{h,cpp}` | present | — | MATCH | MATCH | MATCH |
| `src/utils/{logger,string_utils}.h` | present | — | MATCH | MATCH | MATCH |
| `tests/` (4 hermetic + 2 probes) | present | — | MATCH | MATCH | MATCH |
| `.env.example` | present | — | MATCH blob `ddf564c868f1…` | MATCH | MATCH |
| `.gitignore` | present | — | MATCH blob `ff2e6cbcb09d…` | MATCH | MATCH |

Physical line counts that must equal `git show --stat a8f3fe85` (they do):

| File | Extraction `--stat` | Disk now |
|---|---:|---:|
| `src/core/imt5_client.h` | 176 | 176 |
| `src/core/mt5_types.h` | 571 | 571 |
| `src/core/mt5_manager.cpp` | 1558 | 1558 |
| `src/core/mt5_manager.h` | 207 | 207 |
| `src/core/mt5_http_client.cpp` | 831 | 831 |
| `src/core/mt5_pool.cpp` | 1097 | 1097 |
| `src/core/mt5_watchdog.cpp` | 86 | 86 |
| `CMakeLists.txt` | 173 | 173 |

A12’s “177-line interface” is the same file counted with a trailing newline as a line; `ReadAllLines` / git `--stat` both say **176**. Not a rewrite.

---

## 4. Still real C++, not a stub

`imt5_client.h` still opens as the transport-agnostic contract A12 mapped:

```15:17:D:\Prop\mt5-sdk\src\core\imt5_client.h
class IMT5Client {
public:
    virtual ~IMT5Client() = default;
```

User / deal / group methods that A12 / A85 quoted (`CreateUser`, `GetDeals`, `GetAllGroups`) are still in that header. This agent re-read lines 1–50: `#pragma once`, `mt5_types.h`, `IMTTickSink` forward-decl, `GetEventQueue`, `DealerBalance` / `Deposit` / `Withdraw` — same YoPips-extracted surface.

`mt5_manager.h` is still a native Manager wrapper, not a C# interop shim:

```1:24:D:\Prop\mt5-sdk\src\core\mt5_manager.h
#pragma once

#include <Windows.h>
#include "MT5APIManager.h"
#include "mt5_types.h"
#include "core/imt5_client.h"
...
class MT5Manager : public IMT5Client,
                   public IMTManagerSink,
                   public IMTPositionSink,
                   public IMTDealSink,
                   public IMTOrderSink,
                   public IMTUserSink {
```

`CMakeLists.txt` still:

- requires `nlohmann_json`, `spdlog`, `CURL`
- fatals if `Include/MT5APIManager.h` is missing
- builds `add_library(mt5sdk STATIC …)`
- Windows-only adds `mt5_manager.cpp` / `mt5_pool.cpp` / `mt5_watchdog.cpp`
- copies `MT5APIManager64.dll` + two MetaQuotes managed DLLs via `mt5sdk_copy_runtime_dlls`

That is the extraction-era CMake, not a new generator.

---

## 5. Vendor MetaQuotes SDK still on disk

CMake pin: `MT5_SDK_DIR` default = `${CMAKE_CURRENT_SOURCE_DIR}/vendor/MetaTrader5SDK`.

| Path | Bytes | SHA-256 |
|---|---:|---|
| `vendor/MetaTrader5SDK/Include/MT5APIManager.h` | 133 640 | `00F8F0C82DCAF551A9B21D32CE6351B7B8920AB5084E34BED78D73CE4DCEEB33` |
| `vendor/MetaTrader5SDK/Include/MT5APITypes.h` | 2705 | `87A622E7815F012352E7C9D75ED5F26187DDACC28D7D03368F1A3B5AC2FA652B` |
| `vendor/MetaTrader5SDK/Libs/MT5APIManager64.dll` | 7 185 272 | `51A590CD435B19005621EA5B419E86587C1BA513D4E2138617997F6842B430A9` |
| `vendor/MetaTrader5SDK/Libs/MetaQuotes.MT5ManagerAPI64.dll` | 396 872 | `41A66C5D65BAE8B114737FB18E330B19A424B1B295BC4FCB5FF9DC251AAAEDAB` |
| `vendor/MetaTrader5SDK/Libs/MetaQuotes.MT5CommonAPI64.dll` | 1 046 632 | `DB28E45E082B9FAF86169739B5B08FF725C056A974A7A0A4955B649794C0DD2F` |

Counts: **Include = 80**, **Libs = 12**, **Examples = 895**, **Docs = 3**. Matches the tree listing from this session. DLL sizes match the extraction `--stat` (`MT5APIManager64.dll` Bin 0 → 7 185 272).

---

## 6. C# `src/Mt5` did not eat the C++ SDK

| Surface | Measured |
|---|---|
| `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj` | net8 classlib; refs Domain + Application only; **no** native, CMake, or `mt5-sdk` project reference |
| Product C# files | `FakeMt5BrokerConnector.cs`, `IBrokerConnector.cs`, `Mt5BrokerOptions.cs`, `DeterministicGuid.cs` |
| `FakeMt5BrokerConnector` | in-memory lists; `ConnectAsync` — **not** Manager API |
| Grep `DllImport` / `NativeLibrary` / `MT5APIManager` under `D:\Prop\src` `*.cs` | **zero** product hits |
| Only `mt5-sdk` mention in product C# | comment in `VolumeConverter.cs` (“The comment in mt5-sdk mt5_types.h…”) — **read**, not rewrite |

A04 (same day) already classified C++ as the real Manager layer and C# as a stub/collector. C# has since grown a **fake** connector for first-useful ingestion. That is an **additional** layer. It is not a replacement of `mt5-sdk`.

---

## 7. Cross-check against earlier same-day reports

| Report | Claim about C++ SDK | Still true now |
|---|---|---|
| `PHASE0_AUDIT.md` | `mt5-sdk C++` = `EXISTS_AND_GOOD` | **Yes** |
| `A04_mt5_csharp_vs_sdk.md` | `IMT5Client` real; two transports | **Yes** (same files, same line counts) |
| `A12_imt5_client_map.md` | `imt5_client.h` 1–177, class at 15 | **Yes** (file unchanged) |
| `A85_yopips_extraction.md` | extracted from YoPips; preserve read/subscribe | **Yes** (still that extraction; payments/KYC still absent from `AppConfig`) |
| `A103_gitignore.md` | gitlink `160000 a8f3fe85…` | **Yes** (same SHA) |

No later commit in the nested repo could have invalidated those maps.

---

## 8. Honesty / non-claims

- This is **not** a CMake/MSVC build proof. C20 did not compile `mt5sdk`.
- This is **not** a claim that C++ is wired into `TraderIntelligence.Mt5Worker`. A07 already said it is not referenced. That is **preservation**, not integration.
- This is **not** “≥95% decompiled” or any EX5 claim. Wrong tree.
- `IMT5Client` still carries YoPips dealer methods (`CreateUser`, `Deposit`, `Withdraw`, `SendTrade`). Preserving the tree does **not** authorize copying those onto the C# source connector (A85 law).
- Vendor SDK licence: still MetaQuotes; still not ours to sublicense (`README.md` 170–177).

---

## 9. Commands run (read-only)

```
git -C D:\Prop ls-files -s -- mt5-sdk
git -C D:\Prop status --porcelain -- mt5-sdk
git -C D:\Prop\mt5-sdk rev-parse HEAD
git -C D:\Prop\mt5-sdk status --porcelain=v1
git -C D:\Prop\mt5-sdk diff --stat HEAD
git -C D:\Prop\mt5-sdk ls-files -d
git -C D:\Prop\mt5-sdk hash-object -- <each first-party path>
Get-FileHash -Algorithm SHA256 <first-party + vendor pins>
[System.IO.File]::ReadAllLines(...).Length   # physical lines
```

Product source: **not modified**.

---

## 10. One-line pin

`D:\Prop\mt5-sdk` C++20 library **exists**, gitlink **`a8f3fe85`**, working tree **identical** to the 2026-07-31 YoPips extraction, vendor Manager API **intact**, C# **did not rewrite it**.
