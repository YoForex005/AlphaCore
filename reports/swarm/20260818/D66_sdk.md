# D66 — Confirm `mt5-sdk` C++ was not rewritten

| Field | Value |
|---|---|
| Agent | D66 (senior engineer, preservation re-measure) |
| Date | 2026-08-18 13:41:29 +05:30 (2026-08-18T08:11:29Z) |
| Assigned | Confirm `mt5-sdk` not rewritten. Write this report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D66_sdk.md` |
| Tree | `D:\Prop\mt5-sdk` (nested C++20 CMake repo; parent gitlink) |
| Product source modified | **No.** This report is the only write. `D:\Prop\src`, `D:\Prop\apps`, and `D:\Prop\mt5-sdk` were not edited. |
| Nested HEAD | `a8f3fe85bc0adf109acb5ec72ed8adb2c0a289df` |
| Parent gitlink | mode `160000` → same SHA (`git -C D:\Prop ls-files -s -- mt5-sdk`) |
| Parent HEAD (unrelated) | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback…`, 2026-08-18 13:24:21 +0530) |
| Nested working tree | **clean** vs HEAD (no staged, unstaged, untracked, or deleted tracked files) |
| Nested commits on `main` | **1** (extraction only; no rewrite commit) |
| Adjacent (read, not rewritten) | `C20_sdk_preserved.md` (same-day first pin), `A04`, `A12`, `A85`, `A103`, `D04`, `D67`, `PHASE0_AUDIT.md` |

This is an **independent re-measure**, not a copy of C20. Same conclusion, later clock, corrected 64-hex SHA-256 (C20 prefixed extra digits on several SHA-256 cells; git blobs already matched).

---

## 0. Verdict

**PRESERVED. Not deleted. Not rewritten.**

`D:\Prop\mt5-sdk` is still the **single-commit YoPips extraction** (`a8f3fe85`, 2026-07-31). The nested working tree is **byte-identical** to that commit. Parent `D:\Prop` still pins the same SHA as a `160000` gitlink. The C# collector under `D:\Prop\src\Mt5` is a **separate** net8 fake; it does not replace, P/Invoke, or reimplement the C++ Manager layer.

| Claim | Measured |
|---|---|
| SDK directory deleted | **False.** Tree exists; `project(mt5sdk LANGUAGES CXX)` still in `CMakeLists.txt`. |
| First-party C++ deleted | **False.** 17 `.cpp` + 16 `.h` under `src/`, `config/`, `tests/` (33 files). |
| SDK rewritten in this Prop / D-wave | **False.** Nested repo has **one** commit; `git diff HEAD` empty; **37/37** first-party paths `git hash-object` **MATCH** `git ls-files`; full-tree **1026/1026** hashed tracked blobs MATCH (1 vendor example path skipped by PowerShell `Test-Path` UTF-8; still tracked — §5). |
| Second / rewrite commit | **False.** `rev-list --count HEAD` = **1**. `log --oneline --all` is only `a8f3fe8`. |
| Parent gitlink moved | **False.** `160000 a8f3fe85… 0 mt5-sdk`. Parent porcelain for `mt5-sdk` empty. Parent history for that path is only `6c41447 Initial commit`. |
| C# rewrite of Manager API | **False.** `TraderIntelligence.Mt5` has 4 authored `.cs` files; sole connector is `FakeMt5BrokerConnector`; `ConnectAsync` sets a bool. Zero `DllImport` / `NativeLibrary` / `MT5APIManager` in product C#. |
| Vendor MetaQuotes SDK stripped | **False.** Include **80**, Libs **12**, Examples **895**, Docs **3**. Pin hashes MATCH HEAD. |
| Wired into C# worker | **Not claimed.** Still **not referenced** by `Mt5TraderIntelligence.sln` / `apps/mt5-worker`. Preservation ≠ integration. |

Classification (same as `D:\Prop\reports\PHASE0_AUDIT.md`): **`EXISTS_AND_GOOD`** as the C++ transport. It is **not** the C# Phase-1 collector.

---

## 1. What “not rewritten” means here

Two independent facts were measured on 2026-08-18T08:11Z:

1. **Not deleted** — `D:\Prop\mt5-sdk` exists; CMake still declares `project(mt5sdk LANGUAGES CXX)` / `CMAKE_CXX_STANDARD 20`; `IMT5Client`, `MT5Manager`, `MT5HttpClient` are present and non-empty.
2. **Not rewritten** — the nested repository was not force-pushed, rebased, amended, or edited after extraction. There is no second commit. There is no working-tree delta. Load-bearing physical line counts equal `git show --numstat a8f3fe85`.

NTFS `LastWriteTime` on first-party `.cpp`/`.h` is a **uniform** `2026-08-18 12:32:57 +05:30` (33/33). That is a **checkout/clone stamp**, not a content rewrite: vendor headers share the same second, and every blob still equals `a8f3fe85`. Binding identity is **git**, not mtime.

This agent did **not** rebuild CMake, run tests, or edit any file under `D:\Prop\mt5-sdk` or `D:\Prop\src`.

---

## 2. Git identity (binding)

### 2.1 Nested repo

```
repo:     D:\Prop\mt5-sdk
branch:   main...origin/main
remote:   https://github.com/YoForex005/mt5-sdk.git
HEAD:     a8f3fe85bc0adf109acb5ec72ed8adb2c0a289df
author:   mql5helpline <mql5helpline@gmail.com>
author:   2026-07-31T01:13:45-07:00
commit:   2026-07-31T01:14:24-07:00
subject:  Extract reusable MT5 SDK from the YoPips prop-firm backend
commits:  1
tracked:  1027
deleted:  (none)
untracked:(none)
diff HEAD:(empty)
reflog:   clone from origin → checkout HEAD → checkout main; all a8f3fe8
```

That commit introduced **1027 files / 223 819 insertions** and is still the tip. `origin/main` equals local `main`.

### 2.2 Parent `D:\Prop` records a gitlink, not a copy

```
160000 a8f3fe85bc0adf109acb5ec72ed8adb2c0a289df 0	mt5-sdk
```

No `D:\Prop\.gitmodules`. The directory is a **separate git repo** pinned by SHA. `git -C D:\Prop status --porcelain -- mt5-sdk` is empty — the pin has not been moved. Parent history for path `mt5-sdk` is a single record: `6c41447` (2026-08-18 13:12:17 +0530, `Initial commit`).

Parent working tree has other dirty paths (292 porcelain lines at measure time). **None** of them are `mt5-sdk`.

---

## 3. First-party inventory vs extraction

README “What's in here” vs disk — **17/17 present**. CMake `MT5SDK_SOURCES` + optional Postgres/Drogon + tests + probes — **all paths exist**.

Counts:

| Bucket | Files |
|---|---:|
| `src/` `.cpp` + `.h` | 25 |
| `config/` `.cpp` + `.h` | 2 |
| `tests/` `.cpp` | 6 |
| **First-party C++/H** | **33** (17 `.cpp` + 16 `.h`) |

Physical line counts vs `git show --numstat a8f3fe85` (they match):

| File | Extraction additions | Disk `ReadAllLines` |
|---|---:|---:|
| `CMakeLists.txt` | 173 | 173 |
| `src/core/imt5_client.h` | 176 | 176 |
| `src/core/mt5_types.h` | 571 | 571 |
| `src/core/mt5_manager.h` | 207 | 207 |
| `src/core/mt5_manager.cpp` | 1558 | 1558 |
| `src/core/mt5_http_client.cpp` | 831 | 831 |
| `src/core/mt5_pool.cpp` | 1097 | 1097 |
| `src/core/mt5_watchdog.cpp` | 86 | 86 |

A12’s “177-line interface” is the same file counted with a trailing newline as a line. Not a rewrite.

---

## 4. First-party hashes (measured this agent)

`sha256` is `Get-FileHash -Algorithm SHA256` (64 hex). `head_blob` is `git ls-files -s`. `wt_blob` is `git hash-object`. All **MATCH**.

| Rel path | Bytes | Lines | SHA-256 | HEAD blob | vs HEAD |
|---|---:|---:|---|---|---|
| `CMakeLists.txt` | 6206 | 173 | `98345532CA0D33888E919D14F680B933EB60C6C2A2CE85DBBF1F0D05419719E9` | `31278758d322b95be6f471b2f8663055eeb21b4e` | MATCH |
| `README.md` | 6843 | 177 | `8E62708EB0DA53E483579A78CECE5B7A981BFD1B05CE91D22A81487538A59D5C` | `87266d106256b11512cf58295b9592ed193c24e6` | MATCH |
| `.gitignore` | 482 | 38 | `06D08A304754CE6801C2413C1C05373DA90AAD6803C2F0D16E4BAA4028A67F87` | `ff2e6cbcb09dea637af82e49ed3ef4a102603f92` | MATCH |
| `.env.example` | 4999 | 122 | `937F7CB0A6912A05BEE0E5B672C696D6D4B41F63FFD530D2451C56715020C47C` | `ddf564c868f124c2e88ec61371ffdf3fada929a2` | MATCH |
| `config/app_config.cpp` | 6370 | 157 | `512304425FC42E61563754CF8ED40786E52977A1EF4F975450B1CB6E764FC6BE` | `6fb9117aac365a0c011002d9b9877a41c0ced4cc` | MATCH |
| `config/app_config.h` | 2824 | 65 | `2EE8B969C4B069A340053D6F5A868D1E7E38769F6A8A2AD74C80626D1FF38B83` | `fb82c7293ae1c066be31f24f07b2ad345bbc7347` | MATCH |
| `src/core/imt5_client.h` | 9625 | 176 | `CB8D632BB94ADC1145C0343418788010E6FEDC6886979A59B34E6332B104C707` | `c3d96b42bcd5e0407642bd73916d6df269a1320d` | MATCH |
| `src/core/mt5_types.h` | 25328 | 571 | `1D3BE309AC89141C82EFD8F775812913412B5AA293C9B300D948B65329A99C63` | `bbbb080063c5e538823f588ad115af92c861192c` | MATCH |
| `src/core/mt5_manager.h` | 10363 | 207 | `0C098926BDFD966B91231148EE91FC371F841C84224B223712C3C8EDAB277B79` | `513ed62bd665483b968e0b1c207785998e4cb89c` | MATCH |
| `src/core/mt5_manager.cpp` | 62958 | 1558 | `C25AD8CA9ACFBC5B64AB101C5BCDFCD1CF3CA6FE362BFCD2FC84EDC2EA2AFA98` | `43594571cabfbb44c26513b327bde9535ee99935` | MATCH |
| `src/core/mt5_http_client.h` | 8644 | 217 | `38DBC7EE7E0C8EA637652272C6252626ED43E6B33AD8C39A7521F5BF04E98588` | `7d565cd457c271f92292360302bfe814cf6c1ea2` | MATCH |
| `src/core/mt5_http_client.cpp` | 34509 | 831 | `5D4ED9AAC6D9662B0765507CD8429CAA6A56CB640CE74715E3F237AB2FF83AF6` | `31a121a121c81906b7b11135133ce1f1968f5518` | MATCH |
| `src/core/mt5_pool.h` | 6729 | 171 | `6A4012B8A394C978DD3965280C6C659F8A007F3684D11A5478BA40DD6879D539` | `21ba9834b1ce2bcb87eeaca5063ee3bb242864b9` | MATCH |
| `src/core/mt5_pool.cpp` | 44877 | 1097 | `B37A418B8A0498EC444A2D577FE1DCE78ED3D7E77616F2AA97BC8264AFCA9D69` | `071c68a2ec452e509dbb693e76fa0299575cc28f` | MATCH |
| `src/core/mt5_watchdog.h` | 1218 | 45 | `C1C3A5A7F12B16C1656D3647D5EC8CBCD1015D4BD22DB802897C0D08404D3D03` | `72c5ba2fa3f4ecf752c4f4c29b7ed0c6b3c4d113` | MATCH |
| `src/core/mt5_watchdog.cpp` | 3003 | 86 | `AFDC003ABB07CAACDCAFB0E5793FE8249E06AC92ADAADDCF8F5EB5A810EA09C1` | `b1a184b752ef9c33ad6f513c9016a5ab1cb31629` | MATCH |
| `src/core/mt5_tick_bridge.h` | 10553 | 212 | `B759D636D8F51D24FA15CA1BDA6A65D2E98958CE73193E53AF5ACBC337C91E68` | `b7bf5ae859ea1090a0707bedd09b2e06d1774ec7` | MATCH |
| `src/core/mt5_tick_bridge.cpp` | 14271 | 387 | `F18FB606AE465921D3F80A6507A8615F4FF820EDA048ACDD16CDF042666D5720` | `067d555922c8ca025e96f336838715faa6bcdd8c` | MATCH |
| `src/core/chart_timeframe.h` | 694 | 27 | `75BAEC64DF4F3A83A6907545AB8BE072C0250D723FDB1492154DF71D478D850C` | `e877e2cb5e3248860a7351d676907ed53fd0ecfa` | MATCH |
| `src/core/chart_timeframe.cpp` | 2923 | 91 | `021BAD99C294D7546A38FBB445ABD71967D4B2865FBAEB638392D192E5908D65` | `0ffc0670320536564a23d745d80f7c8b3b6c7afb` | MATCH |
| `src/db/pg_pool.h` | 6871 | 167 | `1709F1523B6741D88DCD50AD0000F554E3BD8B39A2440BDD683659DF1226446C` | `c8bf1f232cb4c1bb785001f10b619a87daddf386` | MATCH |
| `src/db/pg_pool.cpp` | 17417 | 504 | `30B735B4B2BD79AFE2CA58ACE6C45E3B1D5D225CF606983C8F2395E2A099CE7F` | `d3b09ac4c51406c0596604f920ceeb38ae625300` | MATCH |
| `src/services/metrics_service.h` | 31480 | 581 | `2E837282B38FDA45631E137C9EC7CE82D74851EE7DE000B9C96B1B2FEEF85A75` | `762126826a49574178db3a87f8e0bd83fb98c082` | MATCH |
| `src/services/mt5_account_helper.h` | 3487 | 61 | `82627B1ABD280160C92353E0843F54A73D423F0DECDB9F28B6167BD5FB908CAA` | `b12616818d4d3a82992d6b6c96c278016766ce0d` | MATCH |
| `src/services/mt5_account_helper.cpp` | 5081 | 98 | `C491AF955EEE6FD08B7228884485614794D1C820AFCCB2C165A164941610F9A8` | `7b833dbb968eea0b788cfd7c96a0d0083406a4e3` | MATCH |
| `src/services/mt5_ledger_store.h` | 2466 | 77 | `7C3683AC9063A284731AE51E61D1E650C397048230D93E3FC800C2216AA5A15F` | `c9cb8270e2232b687e976e1e36530099615b7c5e` | MATCH |
| `src/services/mt5_ledger_store.cpp` | 5226 | 100 | `0BB2CD478EE1643FE886F6ECD4097742A0C0E759D43F4A4270470041937AF5BF` | `a40d816c3265b2e17ba15d7d9c7141e66a4391cf` | MATCH |
| `src/services/mt5_time_window.h` | 1072 | 28 | `0877597B14F6EA432A778867C8872961D891A15F177E879302910D5E14248A70` | `d887d9fbfba82d1dac968716845806a006ff0914` | MATCH |
| `src/services/mt5_time_window.cpp` | 3961 | 117 | `5683D7B62C0DA508F774993AA32B8F6642D8A8D7368FB41860399DD9E4CF6764` | `a7ff06bf75d0f980c9c3d1bdd7b7872d7a75e500` | MATCH |
| `src/utils/logger.h` | 2490 | 71 | `B420A7C50B2C64560CA61CC640DD051CFE4DB24DC69F6265C4A59105830623EE` | `8db6a40064d2de2ed3e67ff7e749e442046989a4` | MATCH |
| `src/utils/string_utils.h` | 1377 | 42 | `DCE5B6ED672BF14D63E843548E6AF9A591E660ADF863415A317223771F641D26` | `635b59b183040f1b24a5afd092b31a3256a4bc48` | MATCH |
| `tests/mt5_group_probe.cpp` | 5688 | 165 | `040671CAC30929A99181F0C79621B5E2EED36516AF1D8B49DF80B84F0C191E33` | `8db76d4966c4a037043436cab68d1c6f42f83113` | MATCH |
| `tests/mt5_http_client_pool_timeout_test.cpp` | 17629 | 449 | `E600319D752B939DFFDFB42F6840CDFA2DC128CA135BFB352F0E99ED46BD3D14` | `f358e3ef77ad87e8df17288712da163aac328eef` | MATCH |
| `tests/mt5_ledger_store_test.cpp` | 1094 | 35 | `061D87EE4639C6A531EFFDFEBB5D206F09638A025E6652EEE11DBE0375CADD00` | `b6b590b77b1b86aa0573ad503ec72b40669fe7d8` | MATCH |
| `tests/mt5_news_calendar_probe.cpp` | 7733 | 211 | `006BB24D4F16AAE6D7326461D87241326715F034C681EB3745660CFAF14C3874` | `6ad88f9ece1d03732fb2db81635abd1ed6fba7e5` | MATCH |
| `tests/mt5_news_calendar_test.cpp` | 3398 | 89 | `414282DDE22EA23B423FC5338730DF798E64D3685A1166D336EC7CBB82D831E9` | `567d74deb4a7c22369e35ba2142db71531a7dabd` | MATCH |
| `tests/mt5_time_window_test.cpp` | 3839 | 77 | `F0EC2A4E48D9426C90CA62F6B5D5DA3131A22D612089E153232F0FD4619BD900` | `de848ebd82ca8d8fc4e260dd1fdae1aa13a51b25` | MATCH |

Same-day D67 independently published the HTTP / `imt5_client.h` / `mt5_types.h` SHA-256 values above. They match this table.

C20 SHA-256 cells that start with extra digits (e.g. CMake `15298…`, `imt5_client.h` `153CB8…`) are **display errors** in C20. The **git blobs** C20 quoted are the same as this table.

---

## 5. Full-tree blob compare + vendor pins

Every tracked path was compared (`git ls-files` vs `git hash-object`):

| Metric | Value |
|---|---|
| Tracked files | 1027 |
| Hashed + compared | 1026 |
| Blob mismatches | **0** |
| PowerShell `Test-Path` skip | 1 — `vendor/MetaTrader5SDK/Examples/Gateway/UniNewsServer/res/news/russian.Добро пожаловать!.mht` (UTF-8 / illegal-path in Windows PowerShell). **Still tracked:** blob `c5bd578cfcfadb18d7442d25484964f05a0baa6a`. Not first-party. Not a deletion. |

Vendor MetaQuotes pins (still on disk, MATCH HEAD):

| Path | Bytes | SHA-256 | HEAD blob |
|---|---:|---|---|
| `vendor/MetaTrader5SDK/Include/MT5APIManager.h` | 133 640 | `00F8F0C82DCAF551A9B21D32CE6351B7B8920AB5084E34BED78D73CE4DCEEB33` | `de0daf4b50ec2d9d95e2a969d1ccc65f28521e35` |
| `vendor/MetaTrader5SDK/Include/MT5APITypes.h` | 2 705 | `87A622E7815F012352E7C9D75ED5F26187DDACC28D7D03368F1A3B5AC2FA652B` | `05b111d467f6ed0f7bb3be1e631148eb47c073cd` |
| `vendor/MetaTrader5SDK/Libs/MT5APIManager64.dll` | 7 185 272 | `51A590CD435B19005621EA5B419E86587C1BA513D4E2138617997F6842B430A9` | `c7df2a36086fb2743f0937808684a9933aa4636f` |
| `vendor/MetaTrader5SDK/Libs/MetaQuotes.MT5ManagerAPI64.dll` | 396 872 | `41A66C5D65BAE8B114737FB18E330B19A424B1B295BC4FCB5FF9DC251AAAEDAB` | `ea2df882f8f263944a054fbf16a93e07021cbedb` |
| `vendor/MetaTrader5SDK/Libs/MetaQuotes.MT5CommonAPI64.dll` | 1 046 632 | `DB28E45E082B9FAF86169739B5B08FF725C056A974A7A0A4955B649794C0DD2F` | `2334eaa8b564e67ffa442de3253aa954d8f14b03` |

Counts: **Include = 80**, **Libs = 12**, **Examples = 895**, **Docs = 3**. `MT5APIManager64.dll` size still equals the extraction `--stat` (`Bin 0 → 7185272`).

CMake still fatals if `Include/MT5APIManager.h` is missing; still copies the three runtime DLLs via `mt5sdk_copy_runtime_dlls`.

---

## 6. Still the extracted C++ surface (not a stub rewrite)

`imt5_client.h` still opens as the transport-agnostic contract:

```15:17:D:\Prop\mt5-sdk\src\core\imt5_client.h
class IMT5Client {
public:
    virtual ~IMT5Client() = default;
```

`CreateUser` / `DealerBalance` / `Deposit` / `Withdraw` are still on that header (lines 32–47). Preserving the tree does **not** authorize copying those onto the C# collector (A85 law).

`mt5_manager.h` is still a native Manager wrapper:

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

`CMakeLists.txt` still: requires `nlohmann_json` + `spdlog` + `CURL`; Windows-only adds `mt5_manager.cpp` / `mt5_pool.cpp` / `mt5_watchdog.cpp`; `add_library(mt5sdk STATIC …)`. That is the extraction-era CMake, not a new generator.

---

## 7. C# `src/Mt5` did not eat the C++ SDK

| Surface | Measured |
|---|---|
| `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj` | net8 classlib; ProjectReference Domain + Application only; **no** native, CMake, or `mt5-sdk` reference |
| Authored C# in that project | `FakeMt5BrokerConnector.cs` (7049 B), `IBrokerConnector.cs` (1557 B), `Mt5BrokerOptions.cs` (1609 B), `DeterministicGuid.cs` (709 B) |
| `FakeMt5BrokerConnector.ConnectAsync` | `_connected = true; return Task.CompletedTask;` — **not** Manager API |
| `Mt5TraderIntelligence.sln` | **zero** `mt5-sdk` / `mt5sdk` strings |
| Grep `DllImport` / `NativeLibrary` / `MT5APIManager` under `D:\Prop\src` `*.cs` | **zero** product hits |
| Mentions of the C++ tree in product C# | comments only: `VolumeConverter.cs` (“The comment in mt5-sdk mt5_types.h…”); `DealAction.cs` / `DealEntry.cs` (“Mirrors IMTDeal::… in MetaTrader5SDK Include/Bases/…”). **Read, not rewrite.** |

D04 already classified this folder as a demo fake. C# growth is an **additional** layer. It is not a replacement of `mt5-sdk`.

---

## 8. Cross-check against earlier same-day reports

| Report | Claim about C++ SDK | Still true now |
|---|---|---|
| `PHASE0_AUDIT.md` | `mt5-sdk C++` = `EXISTS_AND_GOOD` | **Yes** |
| `C20_sdk_preserved.md` | gitlink `a8f3fe85`; WT identical; not rewritten | **Yes** (re-measured ~16 min later; blobs unchanged) |
| `A04_mt5_csharp_vs_sdk.md` | `IMT5Client` real; two transports | **Yes** (same files, same line counts) |
| `A12_imt5_client_map.md` | `imt5_client.h` class at line 15 | **Yes** |
| `A85_yopips_extraction.md` | extracted from YoPips; preserve read/subscribe | **Yes** |
| `A103_gitignore.md` | gitlink `160000 a8f3fe85…` | **Yes** |
| `D04_mt5_census.md` | C# is Fake; no P/Invoke | **Yes** |
| `D67_http_groups.md` | HTTP `GetGroupDetails` still the extraction stub; SHA-256 match | **Yes** (same hashes) |

No later nested commit exists that could have invalidated those maps.

---

## 9. Honesty / non-claims

- This is **not** a CMake/MSVC build proof. D66 did not compile `mt5sdk`.
- This is **not** a claim that C++ is wired into `TraderIntelligence.Mt5Worker`. A07 / D31 already said it is not referenced. That is **preservation**, not integration.
- This is **not** “≥95% decompiled” or any EX5 claim. Wrong tree.
- `IMT5Client` still carries YoPips dealer methods (`CreateUser`, `Deposit`, `Withdraw`, `SendTrade`). Preserving the tree does **not** authorize copying those onto the C# source connector (A85).
- Vendor SDK licence: still MetaQuotes; still not ours to sublicense (`README.md` 170–177).
- Uniform `LastWriteTime` of `2026-08-18 12:32:57` is **checkout**, not rewrite. Do not treat NTFS mtime as content identity.
- C20 SHA-256 prefixes were sloppy; D66 SHA-256 are 64-hex `Get-FileHash` values. Blobs agree.

---

## 10. Commands run (read-only)

```
git -C D:\Prop ls-files -s -- mt5-sdk
git -C D:\Prop status --porcelain -- mt5-sdk
git -C D:\Prop log --oneline -- mt5-sdk
git -C D:\Prop\mt5-sdk rev-parse HEAD
git -C D:\Prop\mt5-sdk status --porcelain=v1
git -C D:\Prop\mt5-sdk diff --stat HEAD
git -C D:\Prop\mt5-sdk rev-list --count HEAD
git -C D:\Prop\mt5-sdk log --oneline --all --decorate
git -C D:\Prop\mt5-sdk log -1 --format=author/committer ISO
git -C D:\Prop\mt5-sdk ls-files -d
git -C D:\Prop\mt5-sdk ls-files -o --exclude-standard
git -C D:\Prop\mt5-sdk hash-object -- <each first-party + vendor pin>
git -C D:\Prop\mt5-sdk show --numstat a8f3fe85 -- <load-bearing>
Get-FileHash -Algorithm SHA256 <first-party + vendor pins>
[System.IO.File]::ReadAllLines(...).Length
Get-ChildItem counts under src/ config/ tests/ vendor/...
Select-String sln for mt5-sdk
grep DllImport / NativeLibrary / MT5APIManager / IMT5Client under src and apps
```

Product source: **not modified**.

---

## 11. One-line pin

`D:\Prop\mt5-sdk` C++20 library **exists**, gitlink **`a8f3fe85`**, working tree **identical** to the 2026-07-31 YoPips extraction, vendor Manager API **intact**, C# **did not rewrite it**.
