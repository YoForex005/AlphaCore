# Swarm Log

Permanent log of `D:\Prop` research / audit waves. Chat is not storage.

---

## 2026-08-18 — E032 Vite SPA routes return HTTP 200

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:51:00+05:30 / HTTP Date 2026-08-18T08:21:45Z |
| Agent | E032 |
| Purpose | Vite routes returned 200. Write `E032_pages_200.md`. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E032_pages_200.md` |
| Capture | `D:\Prop\reports\swarm\20260818\_tmp_e032\` |
| Product source modified | **No** |
| Listener | `127.0.0.1:3000` node pid **49100** `vite.js --host 127.0.0.1 --port 3000` |
| `App.tsx` SHA-256 | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` |
| Shell SHA-256 | `26270EBBA1F0ED45E5B2362F38589802C1DEB612C59180AD292F7C87E9DF4C6F` (624 B) |
| Verdict | **16/16 destinations GET 200.** HEAD sampled 200. 15/15 page modules 200 JS. Unmapped `/login` `/models` `/api/overview` also 200 HTML (SPA fallback). Only `/favicon.ico` 404. 200 ≠ painted widgets. |

---

## 2026-08-18 — E030 honest live vs demo scorecard

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:16+05:30 (hashes); HTTP 13:51:13+05:30 |
| Agent | E030 |
| Purpose | Write an honest live vs demo scorecard. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E030_honesty.md` |
| Product source modified | **No** |
| Live API | `127.0.0.1:5000` HTTP 200: 4 accounts, 2 SHADOW, 1 RISK_BLOCKED, 0 LIVE, `mt5Healthy` true (lie), FIX `Disconnected`, `realCopyEnabled` false, `shadowPnl` 248.20 (Σ slip) |
| Workers | **Not running.** InMemory book is API-local. |
| Verdict | **Demo YES / Live NO.** §69 accepted **0/12** (demo shape 7/12). §68 **0/19**. §70 **0/14**. Send **SAFE_BY_ABSENCE**. `CanPromoteToLive` false. |

---

## 2026-08-18 — E038 settings featureFlags REAL_COPY false

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:51:56+05:30 / 2026-08-18T08:22:14Z |
| Agent | E038 |
| Purpose | Settings `featureFlags` `REAL_COPY` false. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E038_flag_api.md` |
| Product source modified | **No** |
| Live GET | `http://127.0.0.1:5000/api/settings` **200** `featureFlags.REAL_COPY_EXECUTION_ENABLED=false` (literal in `Program.cs` L45; SHA `61B1E0D1…`) |
| Writes | PUT/PATCH/POST/DELETE `/api/settings` **405** `Allow=GET`; `/api/v1/settings*` **404** |
| Dead twin | `SettingsController` SHA `B19274DC…` unmapped; `LiveCopyEnabled` ≠ architecture name |
| Verdict | Display floor is **false** and correct vs §41. Not a binder. Not a send gate. Do not MapControllers the Redis PUT. |

---

## 2026-08-18 — E039 22 skipped conversion tests

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:51:43+05:30 |
| Agent | E039 |
| Purpose | 22 skipped conversion tests. Write this report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E039_skipped.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Test SHAs | `SourceDestinationQuantityConversionTests` `AA1FA307…` (7344 B / 184); `QuantityNormalizerStepMinMaxTests` `63D2691D…` (5174 B / 162) |
| SUT SHA-256 | `QuantityNormalizer` `B6CC53E8…` (1041 B / 31; unchanged vs D18) |
| `dotnet test` | conversion filter **33 passed / 0 failed / 22 skipped / 55 total** (exit 0) |
| Verdict | **All 22 unit skips are A43 conversion backlog.** 21 = missing `IQuantityConverter` (`Assert.Fail` / `BeFalse`); 1 = E23 raw `MaxQuantity` 5.09 vs FloorToStep 5.00. Passing Facts lock `0.10 → 0.10` (want 10.00). G7/G10 FAIL. Do not un-skip first. |

---

## 2026-08-18 — E019 `BaselineScorerTests` coverage

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:49:27+05:30 |
| Agent | E019 |
| Purpose | List `BaselineScorerTests`. Write coverage inventory vs SUT. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E019_score_cov.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Tests SHA-256 | `61E34A07D76B104CF5D8B818242104522A8B59D12422C5EF4555C2447308D408` (2414 B, 74 lines; untracked) |
| SUT SHA-256 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` (8143 B; unchanged vs D34) |
| `dotnet test` | **3 passed / 0 failed / 0 skipped** (0.3819 s) |
| Verdict | Complete list: `Two_trades_remain_insufficient`; `Three_disciplined_winners_go_to_shadow_not_live`; `Martingale_after_losses_is_risk_blocked`. **7 asserts.** FeatureSnapshot **1/18**. Numeric scores **0/3**. Reachable states **3/5**. A89 scoring/FSM classes **0/21**. Smoke, not A22. |

---

## 2026-08-18 — E018 TradeReconstructionTests inventory + coverage

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:08+05:30 |
| Agent | E018 |
| Purpose | List `TradeReconstructionTests`. Inventory coverage vs A21 / §14–15 / §60 / A89. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E018_recon_cov.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Subject SHA-256 | `CB223DDE3D8FC90BB39C15C8369640B6164A09B7FB30523BF40D8A0BA8E78B9D` (`TradeReconstructionTests.cs`, 4895 bytes) |
| SUT SHA-256 | `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B` (`TradeReconstructor.cs`, 12768 bytes) |
| Measurement | `dotnet test …~TradeReconstructionTests` → **6/6 passed** (smoke) |
| Verdict | **FAIL / INSUFFICIENT.** 6 facts, 26 asserts, 0/25 A21 bit-for-bit, F17 cousin only, 1/22 A89 classes. D33 5-fact census is stale. |

---

## 2026-08-18 — E024 canceled position excluded from first-3 (helper only)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:43+05:30 |
| Agent | E024 |
| Purpose | Answer: is a canceled position excluded from first-3? Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E024_first3.md` |
| Product source modified | **No** |
| Reconstructor SHA-256 | `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B` (unchanged vs D72/D73) |
| Unit fact SHA-256 | `CB223DDE3D8FC90BB39C15C8369640B6164A09B7FB30523BF40D8A0BA8E78B9D` |
| Eval | `D:\Prop\reports\swarm\20260818\_tmp_e024_first3\stdout.txt` (9361 B, SHA `26BABB7F…`) |
| `dotnet test` | cancel/first-3/balance/rollover/client filter **5 passed / 0 failed** |
| Verdict | **Helper YES / production NO.** Extra-ticket 13/14 dirties that `position_id` (`EligibleForFirstThree=false`); `CountCompletedXauUsdTrades` drops it (UNIT helper 2 / false). Score + dashboard + persist ignore the flag (UNIT/M5/SELL_CXL score 3 / true / `SHADOW`; DASH highlights dirty pos 3). Official 0→13 hidden by first-write-wins. C31 C9 / A83 §0 **stale**. |

---

## 2026-08-18 — E026 `/api/health` mapping + demo vs live wording

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:49:57+05:30 |
| Agent | E026 |
| Purpose | Read `/api/health` mapping. Demo vs live wording. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E026_health.md` |
| Product source modified | **No** |
| `Program.cs` SHA-256 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` |
| Verdict | **Anonymous hardcoded inventory at `Program.cs` L26–33. Not a probe. Not A26/A63. Demo-admit + live-deny strings; Achiever/DB still `healthy: true`; QUOTE/redis `false`; `outboxBacklog` literal `0`. Live Manager / live TLS NOT PROVEN.** |

---

## 2026-08-18 — E009 GetTraderDetailAsync + TraderDetailPage vs §51/A93

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:12+05:30 |
| Agent | E009 |
| Purpose | Read `GetTraderDetailAsync` and `TraderDetailPage`. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E009_detail.md` |
| Product source modified | **No** |
| Query SHA-256 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` (`EfDashboardQueries.cs` 8708 B / 205 lines) |
| Page SHA-256 | `C849449B6B76E6E4147AD2503DF00FD5E101C5B5D05ADB7E05708130A8556EB2` (`TraderDetailPage.tsx` 2402 B / 56 lines, untracked) |
| Live HTTP | `GET /api/traders/ACHIEVER/10001` 200 header+3 first-three; miss 99999 = **200 `null`** (not 404/204); `achiever/10001` header + **empty trades** |
| Verdict | **Chrome YES / §51 NO.** Wrapper around A92 row + unbounded `isFirstThree` dump. Page: 8 chips + 4-col table. **0/13** A93 roots, **~2/16** §51 blocks, **1/16** T-tests (T9). D39 204 claim stale. |

---

## 2026-08-18 — E025 DashboardLayout nav vs pages

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:14+05:30 |
| Agent | E025 |
| Purpose | List `DashboardLayout` nav vs `pages/`. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E025_nav.md` |
| Product source modified | **No** |
| Layout SHA-256 | `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` (1854 B, 44 CRLF; unstaged vs HEAD) |
| App.tsx SHA-256 | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` (2062 B, 42 CRLF; unstaged) |
| Verdict | **14/14** nav `to`s join a routed page file. **15** pages; Trader Detail is the only page without a sidebar row (A62-correct). Models + Login still absent. **7/14** labels abbreviated vs §46. `/groups` ≠ A26 `/mt5-groups`. HEAD nav is **12** items and `pages/` is not in git. |

---

## 2026-08-18 — E004 test projects passing vs skipped

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:48:18+05:30 |
| Agent | E004 |
| Purpose | Read test projects and list passing vs skipped. Write this report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E004_tests.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Scratch | `D:\Prop\reports\swarm\20260818\_tmp_e004\` (`unit.trx`, `integration.trx`) |
| Measured | Unit **86 / 64 passed / 22 skipped / 0 failed**; Integration **3 / 3 / 0 / 0**; combined **89 / 67 / 22 / 0** |
| Verdict | Both .sln test projects exit 0. All 22 skips are A43 `IQuantityConverter` / dest re-floor. C17 83/60/1/22 is stale. C++ `mt5-sdk/tests` not built (CMake default OFF). |

---

## 2026-08-18 — E005 architecture risk/copy rules → RiskEngine + tests

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | E005 |
| Purpose | Map architecture risk/copy rules to `RiskEngine` + tests. Write the matrix. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E005_rules_matrix.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| SUT SHA-256 | `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` (8567 B, 189 NL; unchanged vs B13/D13) |
| Tests SHA-256 | `7B95236489E2FE169BFC8E9F57A9E2C89F6F5F047116D9DC82CFA8820FB2DF51` (2909 B, 87 NL; unchanged vs C03/D35) |
| Inventory | **110** rules (R001–R110). Engine reasons **21**. Facts **5**. A89 #50–59 on disk **0/10**. Product `Evaluate` callers **0**. |
| Verdict | Vocabulary stub with the right §64 *names*. **18** MATCH, **22** PARTIAL, **11** STUB_WRONG (red-day freeze, send-under-stop-new, `ReduceSize` qty 0, exclusive kill enum, unsigned mid, unmapped close), **41** MISSING. Live copy **SAFE_BY_ABSENCE**. §68/§70 boxes this file owns stay **unchecked**. |

---

## 2026-08-18 — E022 confirm no `.env`

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:45+05:30 / 2026-08-18T08:20:45Z |
| Agent | E022 |
| Purpose | Confirm no `.env`. Write the report. Do not print secrets. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E022_no_env.md` |
| Product source modified | **No** |
| Root `.env` | **YES** — 3408 B, SHA-256 `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA`, gitignored (`!! .env`), `git hash-object` = HEAD `.env.example` |
| `src\.env` / apps / tests / `mt5-sdk\.env` | **ABSENT** |
| Verdict | Assigned “no `.env`” **REJECTED** at repo root. File is the renamed example (placeholder password slots). Never tracked (`rev-list` empty). Live MT5/FIX still **NOT PROVEN**. |

---

## 2026-08-18 — E011 creds block: no filled .env, no user-secrets, live copy cannot start

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:48:16+05:30 |
| Agent | E011 |
| Purpose | Confirm no usable `.env`, no user-secrets, live copy cannot start. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E011_creds_block.md` |
| Product source modified | **No** |
| Report SHA-256 | `16E39DDD59B73EF474ECA5B156391F23D4C5976CE24EB847A55A9E97FE5AEE19` (20870 B, 360 lines) |
| Verdict | **BLOCKED.** No filled operator `.env` (gitignored `D:\Prop\.env` is the unfilled example, SHA `56C81786…`). User-secrets roots + both worker ID folders **absent**. Process `MT5_PASSWORD` / `CTRADER_FIX_PASSWORD` **absent**. Hosts do not load dotenv. Live copy **cannot start**. |

---

## 2026-08-18 — E006 TradeReconstructor dirty canceled positions

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | E006 |
| Purpose | Read `TradeReconstructor` dirty canceled positions. Write this report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E006_cancel_dirty.md` |
| Product source modified | **No** |
| SUT SHA-256 | `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B` (`TradeReconstructor.cs`, 12 768 B / 347 lines) |
| Tests | `TradeReconstructionTests` **6/6** passed (includes `Canceled_deal_on_a_position_excludes_it_from_first_three`) |
| Eval | `D:\Prop\reports\swarm\20260818\_tmp_e006_cancel\` (reports-only; Domain reference) |
| Verdict | **Helper taint exists; A21 dirty does not.** 13/14 skip the volume book and set `EligibleForFirstThree=false` on **every** lifecycle of that `position_id`. No `Dirty` / `RECON_CANCELED_DEAL` / persist column. Production score + dashboard + shadow use `Completed && IsXauUsd` (`M5` helper 2/false vs score 3/true `SHADOW`). Official 0→13 hidden by first-write-wins upsert. A83/C31 “never dirties” is stale. |

---

## 2026-08-18 — D101 untested recon edges (OUT_BY / zero volume / mixed broker)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D101 |
| Purpose | List untested recon edges: OUT_BY, zero volume, mixed broker. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D101_recon_edges.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| SUT SHA-256 | `TradeReconstructor` `AEA3930B…` (12768 B); tests `CB223DDE…` (4895 B, 6 facts) |
| `dotnet test` | TradeReconstruction + DealReason + VolumeConverter **11 passed / 0 failed** |
| Verdict | **All 3 families untested in product tests.** 0 `DealEntry.OutBy`; 0 tradeable `VolumeNative=0`; 0 `STARWAVEFX` reconstruct. A21 F09/F23/`RECON_ZERO_VOLUME` missing. A89 #6/#13/#19 absent. Z4/Z8 still first-3 poison (eligible stays true). Mixed isolation holds in C31 harness only. C31/D33 stale on cancel only. |

---

## 2026-08-18 — E007 PersistDemoShadowAsync SHADOW only?

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:48:36+05:30 |
| Agent | E007 |
| Purpose | Read `PersistDemoShadowAsync`. SHADOW only? Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E007_shadow.md` |
| Product source modified | **No** |
| Store SHA-256 | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` (12097 B / 338; untracked blob `543c1432…`) |
| Infrastructure build | **GREEN** 0/0 (D47 RED stale) |
| Verdict | **YES for copy/shadow rows; NO for the method.** Hard `state != SHADOW` return before `new CopyIntent` / `new ShadowOrder`; `Status="SHADOW_ONLY"`. Method always writes `ScoreUpdate` outbox for any state. Never `ExecutionIntent` / LIVE / FIX send. Not A24. Seed (same SHA as D48): 6+6 SHADOW rows, 4 outbox. |

---

## 2026-08-18 — D102 emergency flatten vs close (risk edges)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:47:10+05:30 |
| Agent | D102 |
| Purpose | Emergency flatten vs close. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D102_risk_edges.md` |
| Product source modified | **No** |
| `RiskEngine.cs` SHA-256 | `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` (unchanged vs D13/D35/D70/C33) |
| Verdict | **Three close-like ops, one reducing pipe.** Source `CLOSE_EXPOSURE` ≠ remainder flatten (G32) ≠ `EMERGENCY_FLATTEN` run. Engine `EmergencyFlatten` only blocks opens; `IsReducing` passthrough has no dest id / coalesce / flatten qty. `AllowFixSend` requires `None`+`Real` (C3/C4 inverted). Loss/DD freeze both exits. Live book `SAFE_BY_ABSENCE`. Do not implement `docs/risk.md` auto-flatten. |

---

## 2026-08-18 — E012 API `:5000` / web `:3000`

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:48:09+05:30 / 2026-08-18T08:18:29Z |
| Agent | E012 |
| Purpose | API 5000 web 3000. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E012_ports.md` |
| Product source modified | **No** |
| Live binds | `:5000` pid 54468 `TraderIntelligence.Api --urls http://127.0.0.1:5000`; `:3000` pid 49100 `vite --host 127.0.0.1 --port 3000` |
| Verdict | **Intended split, not a conflict.** API **5000**, web **3000**. `/health` 200; Vite `/` 200; CORS Origin `:3000` OPTIONS 204 / GET 200 `*`. Hub negotiate 404. `:5160` gone from worktree, still in HEAD launchSettings. |

---

## 2026-08-18 — E008 DemoSeeder + fix-worker: still forging LoggedOn?

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:48:31+05:30 |
| Agent | E008 |
| Purpose | Re-read `DemoSeeder` and fix-worker. Still forging `LoggedOn`? Write `E008_fix_status.md`. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E008_fix_status.md` |
| Product source modified | **No** |
| Seeder SHA-256 | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` (5082 B / 140 lines) |
| `Worker.cs` SHA-256 | `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` (2093 B / 51 lines) |
| Verdict | **No.** Neither seeder nor worker assigns `LoggedOn`. Both persist `Disconnected` + no-socket LastError. Zero product `Status = FixSessionStatus.LoggedOn` writers. Dashboard health bits stay false. Dest quote `2399.45/2399.85` still invented. Live Logon still **NOT PROVEN**. D22 seeder-LoggedOn is stale. Send still **SAFE_BY_ABSENCE**. |

---

## 2026-08-18 — E003 React route × API endpoint matrix

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:47:06+05:30 |
| Agent | E003 |
| Purpose | List all React routes in `apps/web` and matching API endpoints. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E003_route_matrix.md` |
| Product source modified | **No** |
| `App.tsx` SHA-256 | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` |
| `Program.cs` SHA-256 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` |
| Verdict | **16 React destinations; 15 live unversioned maps; 11/11 hook paths exist on the host; 0 `/api/v1`; 3 pages have no fetch (Shadow/Live/Audit); SignalR `/hubs/dashboard` is 404; `SettingsController` is unmapped.** Demo pairing is not A26/A63. |

---

## 2026-08-18 — D92 volume vote: B14 10k over A81 1e8 default

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:46:01+05:30 |
| Agent | D92 |
| Purpose | Vote A81 constructor default 1e8 vs B14 10 000. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D92_volume_vote.md` |
| Product source modified | **No** |
| Converter SHA-256 | `C6C5E3FD26343532EF047F46D7728A5FED7027B82312A225B9CC3AA881EAC0A2` (1318 B) |
| Eval | `D:\Prop\reports\swarm\20260818\_tmp_d92_vote\stdout.txt` |
| Verdict | **B14.** Compiled `new VolumeConverter().Scale == 10000`. Extractors copy `Volume()`; zero `VolumeExt()` in product C++/C#. A81 1e8 is the official ext scale, not the default. Flip → 10 000× recon undersize / send oversize. |

---

## 2026-08-18 — D78 TradersPage is not the §50 leaderboard

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:45:04+05:30 |
| Agent | D78 |
| Purpose | Read `TradersPage.tsx`. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D78_traders.md` |
| Product source modified | **No** |
| Page SHA-256 | `0AF0FF5BD2EE6B7B4BB06F483B065589A1235FE94EE63B2F4491EC00C510518F` (1604 B, 42 LF lines, untracked) |
| Verdict | **Chrome YES / §50 NO.** 9-column demo table via `useTraders({})` → `GET /api/traders`. Missing ML, Shadow P&L, Live allocation, Last scored. No filters/sort/pager/URL state. Detail link uses broker **code**. B29 numeric-enum claim stale (`JsonStringEnumConverter` on). §69 item 8 still FAIL. |

---

## 2026-08-18 — D76 `types/index.ts` vs live API

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:43:29+05:30 |
| Agent | D76 |
| Purpose | Compare `apps/web/src/types/index.ts` vs live API. Write this file. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D76_types.md` |
| Product source modified | **No** |
| Types SHA-256 | `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` (2905 B, 135 lines; 0 imports) |
| DashboardModels SHA-256 | `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` (8 records) |
| Program.cs SHA-256 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` (`JsonStringEnumConverter` + `GetTraderDetailAsync`) |
| Verdict | **DEPRECATED unused stub.** 0/8 dashboard pairs have field parity. 4/13 TS types match anonymous health/recon/settings. B29 stale (ints / no detail DTO). Do not type hooks from `index.ts`. |

---

## 2026-08-18 — D95 scale: not 5,000 accounts

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:44:43+05:30 |
| Agent | D95 |
| Purpose | Confirm the tree is **not** 5,000 accounts. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D95_scale.md` |
| Product source modified | **No** |
| Fake SHA-256 | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` (unchanged vs D24) |
| Verdict | **Not 5,000. Measured 4 logins** (10001, 10002, 10003, 99001) = **0.08%** of §69.3. 18 canned deals. A79 `SeedFiveThousandAccounts` **MISSING**. Score loops hard-code the same four. Checkpoints unused. README L22 overclaims. Port 5000 / quote-age 5000 / balance 5_000 are **not** a census. §69.3 still **FAIL**. |

---

## 2026-08-18 — D82 AuditPage remesure

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:45:05+05:30 |
| Agent | D82 |
| Purpose | Read `AuditPage.tsx`. Write this file. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D82_auditpage.md` |
| Product source modified | **No** |
| Page SHA-256 | `8DE2F9B0AA9B14798C1C6F548E41837F6B5FF80869A3DBBFC91A13769A8E7B38` (324 B, 8 LF; **untracked** vs HEAD `398a142`) |
| Verdict | **Chrome stub, not a reader.** `/audit` + exact §46 label on worktree. 0 table / hook / `GET /api/v1/audit[/logs]` / writer. C38 page bytes unchanged. Program.cs now `61B1E0D1…` (still no audit map). |

---

## 2026-08-18 — D94 “fix-worker stamps LoggedOn” is anti-evidence (and stale)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:44:41+05:30 |
| Agent | D94 |
| Purpose | Re-measure the sentence “fix-worker stamps LoggedOn”. Treat it as anti-evidence. Write `D94_lie.md`. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D94_lie.md` |
| Product source modified | **No** |
| `Worker.cs` SHA-256 | `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` (2093 B / 51 lines) |
| Verdict | **Assignment sentence FALSE against current bytes.** Worker has 0 `LoggedOn` tokens; stamps QUOTE+TRADE `Disconnected` every 15 s (no socket). Mid-wave forge (`B48033A5…` / `real ? LoggedOn : LoggedOn`) is gone and was **anti-evidence** of Logon. HEAD is the 1 s template. D22 seeder `LoggedOn` is stale (`A6416491…` seeds `Disconnected`). Dashboard still maps `LoggedOn` → healthy (latent). `A101` item 1 / §70.1 still **FAIL**. Send still **SAFE_BY_ABSENCE**. |

---

## 2026-08-18 — D98 MayRetryNewOrderSingle false after send

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:46:11+05:30 |
| Agent | D98 |
| Purpose | `MayRetryNewOrderSingle` false after send? Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D98_noretry.md` |
| Eval | `D:\Prop\reports\swarm\20260818\_tmp_d98_noretry\stdout.txt` |
| Product source modified | **No** |
| FSM SHA-256 | `CDF7B67EB0D032513C2EBF73BC5B3F208F665D6A2A18327E39975198DCF12219` (2177 B, 56 NL; unchanged vs B16/D17) |
| Unit | `Unknown_ack_cannot_retry_new_order` **1/1 Passed** |
| Verdict | **Yes at the helper.** `AfterSendAttempt()` → `SentAcknowledgementUnknown`; `MayRetry==false`; `RequiresReconciliation==true`. Eval `MAY_RETRY_AFTER_SEND=False`. System still unproven (zero callers, no T3 arm, string `ExecutionIntent.Status`). G09 stays FAIL. |

---

## 2026-08-18 — D83 ShadowPortfolioPage: §46 / A26 page?

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:43:57+05:30 |
| Agent | D83 |
| Purpose | Read `ShadowPortfolioPage.tsx`. Is it the §46 / A26 Shadow Portfolio? Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D83_shadowpage.md` |
| Product source modified | **No** |
| Page SHA-256 | `608C8C2D2D0F3FE89EC7632159217191809EB92805051EE9529626B78AE36276` (628 B, 14 lines; unchanged vs C08/D08) |
| Verdict | **Chrome only.** Route `/shadow` + abbreviated nav `Shadow`. No hook, no `GET /api/v1/shadow/portfolio` (A63 **in** v1 — §69 blocker, unlike Live). Six demo `SHADOW_ONLY` rows exist (D48) and are not painted. Copy claiming approved CopyIntent + stale expiry is **false**. NOS-off and “not source ticks” are the only true sentences. |

---

## 2026-08-18 — D93 A57 0/12 inventory is STALE

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:44:45+05:30 / 2026-08-18T08:14:45Z |
| Agent | D93 |
| Purpose | Pin that A57’s 0/12 is a stale *inventory* of an empty tree. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D93_a57_stale.md` |
| Product source modified | **No** |
| A57 SHA-256 | `C1E94C992B28818FAF23D9D6923E2EF56877FE205BA1D64334E5294BC784455E` (36 916 B) |
| D93 SHA-256 | `278EF0B5044D12D67C72316E41D0608427C24F85DBAB5B6EA01233A6412FC6C6` (17 290 B) |
| Current scorecard | D41 (SHA `A9B68AB9…`; hashes unchanged vs this pass) |
| Verdict | **A57 inventory STALE.** `Class1` / weatherforecast / 0 pages / 0 tests / non-compiling plural EF are gone. Demo path exists (items 2, 4–8, 11 + React shell). **§69 accepted still 0/12.** Do not paste A57’s item table. Do not increment the gate. |

---

## 2026-08-18 — D96 harness `123456` must not seed

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:48:00+05:30 |
| Agent | D96 |
| Purpose | Pin: harness `55=123456` must not seed. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D96_id.md` |
| Product source modified | **No** |
| Harness SHA-256 | `99A28D8F3F49028706C75B9C4DC46B4CCB3FF98E90AAED3B4B874DD1B4351616` (8970 B, L141 FLAG) |
| Seeder SHA-256 | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` (5082 B, untracked; `VenueInstrumentId=null`) |
| Verdict | **Must not seed; measured not seeded.** Product `123456` lives only in `FixSimulationHarness`. Mapper/options/apps/Infrastructure: 0 hits. Do not wire harness → quote persist. §69.10 still NO. |

---

## 2026-08-18 — D84 ReconciliationPage vs §54

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:43:40+05:30 |
| Agent | D84 |
| Purpose | Read `ReconciliationPage.tsx`. Write this file. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D84_reconpage.md` |
| Product source modified | **No** |
| Page SHA-256 | `BC036D09A78AECBABD47A8DD9AC0B58E934C7DBDF51930B136545797BEFE8886` (490 B, 12 lines; unchanged vs B20/C08/D08) |
| Git | `?? apps/web/src/pages/` — entire pages tree untracked; HEAD `App.tsx` already imports this module |
| Verdict | **Chrome exists; §54 does not.** Title + one sentence + `JSON.stringify` of `GET /api/reconciliation/status` (`UtcNow` + three zeros). 0/8 §54 widgets. A96 DTO 0 fields. Host map **UNSAFE** (looks like a clean successful reconcile). Nav label `Recon` ≠ §46 `Reconciliation`. |

---

## 2026-08-18 — D77 OverviewPage vs §47

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:43:05+05:30 |
| Agent | D77 |
| Purpose | Read `OverviewPage.tsx`. Write the Overview close-read vs architecture §47. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D77_overview.md` |
| Product source modified | **No** |
| Page SHA-256 | `6497193F190445CCF76AED218E01E3BC85050238CB89D06002837FC9C502825C` (2078 B, 35 LF; **untracked** vs HEAD `398a142`) |
| Live `GET /api/overview` | HTTP 200: 4 accounts, 2 “connected”, 3 XAU, 2 SHADOW, 1 RISK_BLOCKED, `shadowPnl` 248.20, `mt5Healthy` true, FIX bits false, `realCopyEnabled` false |
| Verdict | **File exists; §47 is not implemented.** 11/18 dedicated tiles + merged QUOTE/TRADE. `live` / `xauGross` / `xauNet` dropped. MT5 OK is `brokers.Enabled > 0`. A91/A62/A29 “page MISSING” is stale. |

---

## 2026-08-18 — D74 API `JsonStringEnumConverter`

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D74 |
| Purpose | Does the API use `JsonStringEnumConverter`? Write this report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D74_enums.md` |
| Product source modified | **No** |
| `Program.cs` SHA-256 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` (4731 B / 95 lines; same as D06/D30) |
| Verdict | **YES.** One `ConfigureHttpJsonOptions` registration, default ctor (`namingPolicy: null`, `AllowIntegerValues: true`). Live enum fields serialize as identifier strings (`"WATCH"`, `"Long"`). B10/B29 “no converter / integers” is stale. Kill-switch is already `ToString()` (`StopNewExecution`, not A48 `STOP_NEW_EXECUTION`). |

---

## 2026-08-18 — D75 launchSettings weather leftover

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:43:29+05:30 |
| Agent | D75 |
| Purpose | Is there a `launchSettings` weather leftover? Write this report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D75_launch.md` |
| Product source modified | **No** |
| API `launchSettings.json` SHA-256 | `BC0228981D29FFCF8D94737AF3446D96AB29415ACFF494687250265E266CD7F0` (1125 B, 13:32:01) |
| Verdict | **No leftover on the worktree.** 0 `weatherforecast` in all 3 launch files; API profiles are `swagger` ×3. C04/C15 IIS leftover **closed** (same SHA as D06). `HEAD` blob `36903867…` still has 3× `weatherforecast` + `:5160`. `swagger` without `UseSwaggerUI()` is a 404 half-migration, not weather. |

---

## 2026-08-18 — D97 CanPromoteToLive is false

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:45:18+05:30 |
| Agent | D97 |
| Purpose | Confirm `CanPromoteToLive` is false. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D97_nolive.md` |
| Product source modified | **No** |
| SUT SHA-256 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` (unchanged vs D12/D34) |
| `dotnet test` | `BaselineScorerTests` **3 passed / 0 failed / 0 skipped** |
| Verdict | **CONFIRMED false.** `CanPromoteToLive(TraderState current) => false` (L211). Parameter discarded. Product callers: **none**. One unit fact locks `SHADOW` only. Vacuous lock, not A22 R5-before-R6. Persist copies `SuggestedState` blindly. §68 0/19 and §70 0/14 unchanged. |

---

## 2026-08-18 — D87 Infra → Mt5 layering

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D87 |
| Purpose | Answer “Infra references Mt5 OK?” with a re-measured layering census. |
| Artifact | `D:\Prop\reports\swarm\20260818\D87_layer.md` |
| Product source modified | **No** |
| Infra csproj SHA-256 | `4DABF29CA934261CFC46C72514CB7AA04D5E8F9CC8FFAC1BA051BF0CD0668EED` (1035 B, unchanged vs C35) |
| DI SHA-256 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` (unchanged vs C35) |
| Verdict | **YES for Fake demo; NO as A54/go-live graph.** Class `EXISTS_NEEDS_REFACTOR`. Persistence/dashboard still 0 Mt5 usings. Dual `CreateDefault()` remains. API + FIX-worker load `TraderIntelligence.Mt5.dll` transitively. Invert before native Manager lands in `src/Mt5`. Not a §69 FAIL. |

---

## 2026-08-18 — D72 first-3 is completed XAU only (helper), increment not done

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D72 |
| Purpose | Answer: is first-3 reconstructed completed XAU only? Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D72_first3.md` |
| Product source modified | **No** |
| Reconstructor SHA-256 | `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B` (12768 B) |
| Eval | `D:\Prop\reports\swarm\20260818\_tmp_d72_first3\stdout.txt` |
| Verdict | **Helper YES / engine NO / increment NO.** `CountCompletedXauUsdTrades` = `Completed && IsXauUsd && EligibleForFirstThree` (2 XAU + 1 EUR → count 2). `Reconstruct` still emits EUR/XAG. No `first3_keys`. Score + dashboard ignore dirty (`M5` helper 2 / score 3). Over-map `XAUUSDFUT`/`GOLD.` counts as XAU. |

---

## 2026-08-18 — D62 root `.gitignore` recensus

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:40:18+05:30 |
| Agent | D62 |
| Purpose | Read `.gitignore`. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D62_gitignore.md` |
| Product source modified | **No** |
| `.gitignore` SHA-256 | `FAE817C1C2F9AD9BEA4353D89A82ED015585A449FC1339561F2C966A0C2B21E0` (1107 B, 73 lines, LF; HEAD blob `f4c00707…`; clean vs `398a142`) |
| Verdict | **EXISTS_NEEDS_REFACTOR.** Env rules work (`.env` ignored; `!.env.example` correct). A103 §6 unapplied. Worktree deleted `.env.example`; same blob is ignored `.env` (placeholders only). Dirty API `FileStorePath=./fixstore` + `FileLogPath=./fixlogs` are **OPEN**. |

---

## 2026-08-18 — D70 STOP_NEW vs FLATTEN distinct?

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:42:01+05:30 |
| Agent | D70 |
| Purpose | Are `STOP_NEW` and `FLATTEN` distinct? Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D70_kill.md` |
| Product source modified | **No** |
| Verdict | **Specified YES / implemented NO.** §40 two independent controls; tree stores exclusive `KillSwitchMode`. Flatten does not flatten. `{stop-new ON × flatten ACTIVE}` unrepresentable. §68 / §70.13 stay `[ ]`. |

---

## 2026-08-18 — D79 FixSessionsPage: password shown?

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:42:52+05:30 |
| Agent | D79 |
| Purpose | Read `FixSessionsPage.tsx`. Answer: password shown? Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D79_fixpage.md` |
| Product source modified | **No** |
| Page SHA-256 | `EC93326688719E10D3ED5CB275D9BF1E7113C7F61EEA99803F42E1EA268BB886` (1312 B, 26 LF; untracked vs HEAD `398a142`) |
| Verdict | **No. Password is not shown.** Line 8 is the disclaimer only. No `s.password`, no input, no `JSON.stringify`. `FixSessionDto` / `FixSessionState` have no password field. `CTraderFixOptions.Password` is off this path. |

---

## 2026-08-18 — D41 §69 FUV scored against CURRENT repo

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D41 |
| Purpose | Score architecture §69 12 items against current worktree, not A57 stale inventory |
| Artifact | `D:\Prop\reports\swarm\20260818\D41_fuv_now.md` |
| Product source modified | **No** |
| Verdict | **Accepted 0/12.** DEMO: 2, 4, 5, 6, 7, 8, 11. FAIL: 1, 3, 9, 10. PARTIAL: 12. FIX worker now honestly `Disconnected`; shadow persist writes from invented dest quote (`VenueInstrumentId=null`). Live MT5 / QUOTE / discovered tag 55 still absent. |

---

## 2026-08-18 — D48 ShadowOrders in seeder?

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D48 |
| Purpose | Are `ShadowOrders` created in seeder? |
| Artifact | `D:\Prop\reports\swarm\20260818\D48_shadow_rows.md` |
| Eval | `D:\Prop\reports\swarm\20260818\_tmp_d48_shadow\stdout.txt` |
| Product source modified | **No** |
| Verdict | **YES as a rebuild side-effect, not a direct seeder insert.** `DemoSeeder` has no `ShadowOrders` token. First `SeedAsync` → `RebuildTraderAsync` → `PersistDemoShadowAsync` writes **6** `shadow_orders` + **6** `SHADOW_ONLY` intents (10001×3, 99001×3). 10002/10003 get none. Dashboard `ShadowPnl=248.20` is Σ slippage, not P&L. Not §24. |

---

## 2026-08-18 — D51 migrations folder (none)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D51 |
| Purpose | Answer “Migrations folder?” with a measured census. |
| Artifact | `D:\Prop\reports\swarm\20260818\D51_migrations.md` |
| Product source modified | **No** |
| `TraderDbContext.cs` SHA-256 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` (5951 B, unchanged vs C29/D19) |
| Verdict | **MISSING.** No `Persistence/Migrations/` (disk or git). A30 **0/15**. `EnsureCreatedAsync` × 3. Default InMemory. API `ConnectionStrings:Postgres` unused (DI reads `TraderIntelligence` / `DATABASE_URL`). HEAD 5 stub configs deleted; not migrations. §60 / §72.3 **FAIL**. |

---

## 2026-08-18 — D43 §70 live FIX all FAIL

| Item | Value |
|---|---|
| Date | 2026-08-18T08:10:29Z |
| Agent | D43 |
| Purpose | Confirm architecture §70 (14 live FIX acceptance items) all FAIL for **live**. Re-measure tree; do not inherit A101 worker-LoggedOn narrative. |
| Artifact | `D:\Prop\reports\swarm\20260818\D43_s70.md` |
| Product source modified | **No** |
| Verdict | **0 / 14 FAIL.** Same integer as A101. Worker/seeder now stamp `Disconnected` (D32) — honesty, not Logon. No QuickFIX/n, no `GuardedNewOrderSingle`, no `tests/Fix`, no `LOGON_OK`. Domain helpers (RiskEngine / ClOrdId / FSM) are not a send path. `SAFE_BY_ABSENCE` ≠ pass. Live copy stays off. |

---

## 2026-08-18 — D56 `mt5_xau_ticks` table

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D56 |
| Purpose | Re-measure whether `mt5_xau_ticks` exists; write this file. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D56_ticks.md` |
| Product source modified | **No** |
| `TraderDbContext` SHA-256 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` (unchanged vs C60/D19) |
| Verdict | **MISSING.** No entity, no `DbSet`, no `ToTable`, no migration, no `.sql`, no C++/C# writer. Exact MFE **UNAVAILABLE**. Scorer omission is correct (`Unavailable` + null averages). Do not stand in dest quotes or deals. C60 still holds. |

---

## 2026-08-18 — D47 CopyIntent after score SHADOW

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:38:00+05:30 |
| Agent | D47 |
| Purpose | Is CopyIntent created after score SHADOW? |
| Artifact | `D:\Prop\reports\swarm\20260818\D47_copyintent.md` |
| Product source modified | **No** |
| Verdict | **YES by control flow.** `RebuildTraderAsync` persists score then `PersistDemoShadowAsync`; that method `new CopyIntent` only when `state == SHADOW` (plus dest quote). Demo OPEN backfill, not A24. Infrastructure build **RED** (entity rewrite 13:37 vs writer 13:35). C59 writers claim **stale**. |

---

## 2026-08-18 — D63 docker-compose: MT5 not in Linux

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:40:45+05:30 |
| Agent | D63 |
| Purpose | Read `docker-compose.yml`. Confirm MT5 is **not** in Linux. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D63_compose.md` |
| Product source modified | **No** |
| Compose SHA-256 | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` (687 B, 30 LF, LastWriteUtc `2026-08-18T07:48:40.1339443Z`; unchanged vs B37/C12) |
| Docker CLI | **MISSING** (`docker` / `docker-compose` not on PATH) |
| Verdict | **CONFIRMED: MT5 is not in Linux.** Services are `postgres`, `redis`, Linux `api` only. No `mt5-worker`. Line 30: stay-on-Windows comment. Native Manager PE `0x8664`. |

---

## 2026-08-18 — D50 API MapHub / SignalR hub

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:40:00+05:30 |
| Agent | D50 |
| Purpose | Answer **API map hub?** Confirm whether `apps/api` calls `MapHub` / exposes `/hubs/ops` |
| Artifact | `D:\Prop\reports\swarm\20260818\D50_signalr.md` |
| Product source modified | **No** |
| `Program.cs` SHA-256 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` (4731 B; same as D06) |
| Verdict | **No hub mapped.** `AddSignalR` 0 / `MapHub` 0 / no `OpsHub` / no `/hubs/ops`. Unused `SignalR.Common` 8.0.4. Client still dials forbidden `/hubs/dashboard` and swallows failure. Workers correctly do not host SignalR. C28 conclusion holds; C28 hashes and D06 “no Controllers/” are stale. |

---

## 2026-08-18 — D37 SeedingAndStoreTests integration recensus

| Item | Value |
|---|---|
| Date | 2026-08-18 13:38 +05:30 |
| Agent | D37 |
| Purpose | Read `tests/Integration/SeedingAndStoreTests.cs`. Recensus vs §60 / A90 / C16. |
| Artifact | `D:\Prop\reports\swarm\20260818\D37_integ.md` |
| Product source modified | **No** |
| Test SHA-256 | `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD` (unchanged vs C16) |
| Verdict | **PARTIAL** InMemory smoke. §60 **0/8**. Fresh rebuild **RED** (`CS8858` on `ReconstructedTradeResult`). Stale-bin 2/2 class facts PASS. `NotBe(LIVE)` vacuous; unique index unproven. Seeder now `Disconnected` (D22 stale) but test does not lock status. |

---

## 2026-08-18 — D54 Serilog package used?

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:38:57+05:30 |
| Agent | D54 |
| Purpose | Confirm whether the Serilog package is used. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D54_serilog.md` |
| Product source modified | **No** |
| API csproj SHA-256 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` (803 B; HEAD = worktree) |
| `Program.cs` SHA-256 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` (4731 B; matches D06) |
| `appsettings.json` SHA-256 | `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` (1254 B; dirty vs HEAD) |
| Verdict | **Package YES / used NO.** `Serilog.AspNetCore` 8.0.2 is the only product Serilog reference (API). Zero C# call sites (0/85). Worktree `"Serilog"` JSON is unread (C25 “no JSON” is stale). Workers have no package/DLLs. Pipeline / §57 / A50 **MISSING**. |

---

## 2026-08-18 — D33 TradeReconstructionTests coverage gaps

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D33 |
| Purpose | Read `TradeReconstructionTests.cs`. Inventory coverage gaps vs A21 / §14–15 / §60 / A89. |
| Artifact | `D:\Prop\reports\swarm\20260818\D33_recon_tests.md` |
| Subject SHA-256 | `5D99BA22B0FEFC248568E6CB0B462A31126DF825F57D34F9DD8C1586B661FBF2` (`TradeReconstructionTests.cs`, 3939 bytes) |
| SUT SHA-256 | `E20457B398DB6CCC5F78ADE295A340CBC0646F5668F9F79F6AFBCC09D35741DD` (`TradeReconstructor.cs`, 12307 bytes) |
| Product source modified | **No** |
| Test source modified | **No** |
| Measurement | `dotnet test …~TradeReconstructionTests` → **5/5 passed** (smoke) |
| Verdict | **FAIL / INSUFFICIENT.** 5 fused smokes; **0/25** A21 fixtures; **1/22** A89 recon classes on disk; INOUT money double-count unguarded. |

---

## 2026-08-18 — D39 hooks.ts vs Program.cs endpoints

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:37:27+05:30 |
| Agent | D39 |
| Purpose | Compare `apps/web/src/api/hooks.ts` HTTP paths to `apps/api/Program.cs` maps |
| Artifact | `D:\Prop\reports\swarm\20260818\D39_hooks.md` |
| hooks.ts SHA-256 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` (1935 B, unchanged vs D08/B30) |
| Program.cs SHA-256 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` (4731 B; grew vs C04 `E914FA98…`) |
| Product source modified | **No** |
| Verdict | **11/11** hook GETs match a live `MapGet`. **11/15** host maps have a hook. **0/11** use `/api/v1`. Host-only: `/health`, `/ready`, `/api/risk/status`, `POST /api/ops/resync`. SignalR `/hubs/dashboard` has **no** `MapHub`. Trader detail now returns `TraderDetailDto` (`GetTraderDetailAsync`) — B30/C04/D02 stale. |

---

## 2026-08-18 — D30 API endpoints + secrets

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:35:15+05:30 |
| Agent | D30 |
| Purpose | Read `apps/api/Program.cs`. List endpoints. Secrets? |
| Artifact | `D:\Prop\reports\swarm\20260818\D30_api.md` |
| Product source modified | **No** |
| `Program.cs` SHA-256 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` (4731 bytes, 95 lines) |
| Verdict | **15** maps (14 GET + 1 POST), all anonymous. `weatherforecast` **GONE**. No §55 secret on the wire (safe by absence, no sanitizer). `CTrader:Password` empty; `AccountId` `1369850` committed. `POST /api/ops/resync` + CORS `*` **UNSAFE** as an ops door. |

---

## 2026-08-18 — D38 App.tsx + DashboardLayout routes

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:37:02+05:30 |
| Agent | D38 |
| Purpose | Read `App.tsx` and layout; census every React route vs A26 §5.2 / architecture §46 |
| Artifact | `D:\Prop\reports\swarm\20260818\D38_routes.md` |
| Product source modified | **No** |
| `App.tsx` SHA-256 | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` (2062 B, 42 lines) |
| Layout SHA-256 | `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` (1854 B, 44 lines) |
| Verdict | **EXISTS_NEEDS_REFACTOR.** 16 destinations, 14 sidebar links, 15/15 page imports resolve. A26 exact paths **14/17** (`/login` missing, `/models` missing-by-design, `/groups` ≠ `/mt5-groups`). No catch-all, no auth, no header strip. Live+Audit routes exist on worktree only (unstaged +4/+2 vs HEAD). |

---

## 2026-08-18 — D27 FixMessageParser review

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D27 |
| Purpose | Read `FixMessageParser.cs`. Write parser review. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D27_parser.md` |
| Product source modified | **No** |
| SUT | `src/Fix.CTrader/Parsing/FixMessageParser.cs` — 145 lines / 6016 bytes / SHA-256 `C58681E761D43052B53D2A8D00883C461A9E3CEB5B7DF8995D50F8155F710E3D` |
| Eval | `reports/swarm/20260818/_tmp_d27_parser/stdout.txt` (project-referenced `dotnet run`) |
| Verdict | **EXISTS_NEEDS_REFACTOR** as pipe fixture; **UNSAFE** as wire decoder / MD decoder / live outbound. Checksum 163 proven. `Build` always emits `\|\|10=`. Last-wins map. A89 #60/#61/#74 **MISSING**. Zero tests. |

---

## 2026-08-18 — D34 BaselineScorerTests surface

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D34 |
| Purpose | Read `BaselineScorerTests.cs`. Inventory asserts vs SUT. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D34_score_tests.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Tests SHA-256 | `61E34A07D76B104CF5D8B818242104522A8B59D12422C5EF4555C2447308D408` (2414 bytes) |
| `dotnet test` | 3 passed / 0 failed / 0 skipped (2026-08-18 13:36:43) |
| Verdict | 3 facts / 7 asserts lock B35 qualitative trio only. No numeric gold. No A22. No WATCH/EARLY_SCORE/N=0. Winning martingale SHADOW hole unlocked. `AfterHighEarlyScore` never called. A89 #26–#41 / #75–#79 not on disk. |

---

## 2026-08-18 — D07 workers census

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D07 |
| Purpose | Inventory `apps/mt5-worker` and `apps/fix-worker` |
| Artifact | `D:\Prop\reports\swarm\20260818\D07_workers_census.md` |
| Product source modified | **No** |
| Verdict | Two net8 Worker hosts, 6 product files each. mt5-worker: 30 s Fake ingest + score of 4 logins (SHA `57499700…`). fix-worker: 15 s stamps QUOTE/TRADE **Disconnected** (SHA `92A8F492…`; B07/C07 `B48033A5…` stale). A64 jobs **0/7** + **0/10**. No health port, no outbox, no QuickFIX, no Manager DLL. Real send **SAFE_BY_ABSENCE**. Default store InMemory. |

---

## 2026-08-18 — D06 apps/api census (no weatherforecast route)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D06 |
| Purpose | Inventory `D:\Prop\apps\api`. Confirm **no weatherforecast route**. |
| Artifact | `D:\Prop\reports\swarm\20260818\D06_api_census.md` |
| Product source modified | **No** |
| `Program.cs` SHA-256 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` (4731 bytes) |
| Verdict | **Confirmed: no weatherforecast route.** 0 `MapGet("/weatherforecast")`, 0 `WeatherForecast` type, 0 product-source string under `apps/api` (`.http` + all launch profiles included), 0 Debug DLL hits, 0 hits in `apps`/`src`/`tests` authored files. Host is **15** anonymous unversioned maps (`14` GET + `1` POST `/api/ops/resync`) on `:5000`. `/api/v1` **MISSING**. IIS leftover `launchUrl=weatherforecast` (C04/C15) is **gone** (now `swagger`). Do not treat this as first-useful v1. |

---

## 2026-08-18 — D05 Fix.CTrader census

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D05 |
| Purpose | Inventory `D:\Prop\src\Fix.CTrader` (files, types, packages, consumers, HEAD vs worktree) |
| Artifact | `D:\Prop\reports\swarm\20260818\D05_fix_census.md` |
| Product source modified | **No** |
| Verdict | **EXISTS_NEEDS_REFACTOR.** 4 product `.cs` (options, pipe parser, in-memory fence, unused harness). Official QuickFIX/n **absent**. Session types **0/2**. Assembly types have **0** external call sites. Live `NewOrderSingle` **SAFE_BY_ABSENCE**. A05 `Class1` snapshot is stale. |

---

## 2026-08-18 — D21 EfDashboardQueries catalog

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D21 |
| Purpose | Read `EfDashboardQueries.cs`. Catalog every method, field source (query vs literal), table touch, and API map. |
| Artifact | `D:\Prop\reports\swarm\20260818\D21_queries.md` |
| Product source modified | **No** |
| SUT | `src/Infrastructure/Dashboard/EfDashboardQueries.cs` — 168 lines / 7407 bytes / SHA-256 `37A4DDD23305708566888F0BBE2A6CC5DE253FB7151BDEE848195DE715EF4ACE` (unchanged vs C36) |
| Verdict | **EXISTS_NEEDS_REFACTOR** on demo; **UNSAFE** as a 5k read plane. 7/7 port methods wired. ~28–32 sequential SQL on a cold paint. 16 DTO fields hardcoded. Groups N+1; leaderboard full-table + O(n²) join + no page; `GetTraderAsync` reloads the leaderboard; latest-quote / reject / shadow-sum lack supporting indexes. 0 tests. Same SHA as C36. |

---

## 2026-08-18 — D25 duplicate collector ports

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D25 |
| Purpose | Compare `IBrokerConnector` vs `IMt5BrokerConnector`; pick one surface |
| Artifact | `D:\Prop\reports\swarm\20260818\D25_dup_iface.md` |
| Product source modified | **No** |
| Verdict | Keep Application `IMt5BrokerConnector`. Delete unused `src/Mt5/Connectors/IBrokerConnector.cs` (+ `Mt5BrokerEvent`). Zero implementors / consumers. B24 SHA-256 values unchanged. Winner is still incomplete vs A58/§6. |

---

## 2026-08-18 — D19 TraderDbContext vs architecture §45

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D19 |
| Purpose | Re-measure `TraderDbContext` `DbSet`/`ToTable` names against architecture §45 (43-table full initial set) |
| Artifact | `D:\Prop\reports\swarm\20260818\D19_dbcontext.md` |
| Subject SHA-256 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` (`TraderDbContext.cs`, 5951 bytes) |
| Product source modified | **No** |
| Verdict | **FAIL.** **18 / 43** §45 tables present by name; **25** missing; **2** extra (`execution_intents` keep / `kill_switches` not §45). Table-name coverage **41.9%**; A20/A61 completeness **0/43**. B19 counts unchanged. 0 configurations, 0 named UNIQUEs, 0 FKs, 0 migrations. |

---

## 2026-08-18 — D08 web page census

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:33:46+05:30 |
| Agent | D08 |
| Purpose | Inventory `D:\Prop\apps\web\src` and list every page |
| Artifact | `D:\Prop\reports\swarm\20260818\D08_web_census.md` |
| Product source modified | **No** |
| Verdict | **15** page modules, **16** routed destinations (`/` → `/overview` + 15 pages), **14** sidebar links. Import graph 15/15. No orphans. No `LoginPage` / `ModelsPage`. Groups lives at `/groups` not A26 `/mt5-groups`. Live/Audit/Shadow are stubs. Recon/Health/Settings are `JSON.stringify` dumps. Same 15 SHAs as C08. |

## 2026-08-18 — Wave D (100+ agents, standing order)

User: **100+ sub agents always**. Launched **D01–D103**. Orchestrator also stopped forging FIX `LoggedOn`, added `DealReason` skip for rollover/service, trader-detail payload, and demo shadow book for `SHADOW` only.

---

## 2026-08-18 — D22 DemoSeeder LoggedOn without FIX

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D22 |
| Purpose | Read `DemoSeeder.cs`. Note TRADE `LoggedOn` (and QUOTE `ReadyForMarketData`) with no FIX session. |
| Artifact | `D:\Prop\reports\swarm\20260818\D22_seeder.md` |
| Product source modified | **No** |
| Seeder SHA-256 | `139D8F872DC473F0C5381AF2393BDBBE60E1D9A2A5179DD1D1737E04CCC00BEF` (4942 bytes) |
| Verdict | **FORGED.** `DemoSeeder` line 90 writes `FixSessionStatus.LoggedOn` on TRADE; line 73 writes `ReadyForMarketData` on QUOTE. No `Fix.CTrader` call, no TLS, no `35=A`. Dashboard `QuoteHealthy`/`TradeHealthy` become true. Live host `live-us-eqx-01.p.c-trader.com` + `live.pepperstone.1369850` are literals. Confirm C43: live Logon still **NOT PROVEN**. |

---

## 2026-08-18 — C47 next increment (plan only)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | C47 |
| Purpose | Propose next increment: Windows live MT5 connect, QuickFIXn net8 QUOTE logon, EF migrations, RBAC |
| Artifact | `D:\Prop\reports\swarm\20260818\C47_next_increment.md` |
| Product source modified | **No** |
| Verdict | Increment **C47 / I-Live-Foundation**, four slices in order: **47.1** versioned EF migrations (replace `EnsureCreated`); **47.2** first-useful RBAC + audit writer; **47.3** Windows `mt5-collector` ×2 wrapping preserved `IMT5Client` + C# HTTP client; **47.4** official `QuickFIXn.Core`+`QuickFIXn.FIX44` **1.14.1** QUOTE TLS Logon only. Live `35=D` stays off. §69 stays ≤2/12 even if all exits measure; §68 stays 0/19. |

---

## 2026-08-18 — Wave 1 (Phase 0 §73 audit + binding specs)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Wave | **1** |
| Purpose | Architecture §73 A–D *before* large implementation: repository audit, gap analysis, implementation sequence, risk list; plus official cTrader FIX research and first-useful-version specs |
| Law | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` |
| Report dir | `D:\Prop\reports\swarm\20260818\` |
| Index | `D:\Prop\reports\INDEX.md` |
| `reports/agents/` | empty |
| Product source modified by cataloger | **No** |
| Product source modified by wave-1 report agents | **No** (reports only; later code already on disk is from other work, not these markdowns) |

### Inventory (catalog snapshot)

| Band | Count | Notes |
|---|---:|---|
| A01–A105 | **105** | Consecutive; no missing A IDs |
| B-series markdown | **34** | B01–B10, B12–B16, B18–B27, B29–B33, B37–B40 |
| C-series markdown | **2+** | C06, C07 present; rest still landing |
| Scratch | `_tmp_b35_score/` | Not a report (throwaway compile) |
| **Report total** | **141+** | All under `swarm/20260818/` |

B-band gaps at catalog time: B11, B17, B28, B34, B35. **B36 landed** (`B36_risk_fixtures.md`). C-band just started. Do not treat missing IDs as written.

### What wave 1 produced

1. **§73.A Repository audit** — A01–A19, A29, A55, A57, A100, A101; B01–B03, B05–B06, B09 refresh the measured tree after Domain/Application grew.
2. **§73.B Gap analysis** — A29 (Phase 0 / early Domain vocabulary). B02/B03/B05 reclassify layers that A01–A05 still called `Class1`.
3. **§73.C Implementation sequence** — A30 (exact files / modules / migrations for §69).
4. **§73.D Risk list** — A56 (SDK, Windows DLL, ticks, FIX headers, sizing, live-account safety).
5. **Official cTrader FIX** — A31–A36 (overview, RoE, send/recv, FAQ, QuickFIX/n 1.14.1, cServer dictionary).
6. **MT5 SDK binding** — A12–A18, A37–A39, A81–A85, B14 (volume default **10 000**).
7. **First-useful specs** — reconstruction, scoring, risk, shadow, FIX session, outbox, workers, dashboard DTOs A91–A97, indexes, Redis keys, flags, kill switch.

### Honest measured scoreboard

| Gate | Score | Source |
|---|---|---|
| First useful version (§69) | **0 / 12** | A57 |
| Go-live gates (§68) | **0 PASS / 19 FAIL** | A100 |
| Live FIX acceptance (§70) | **0 / 14 FAIL** | A101 |
| Domain compile | **0 errors / 0 warnings** | B01 |
| Live passwords in tree | **NONE FOUND** | A19 |
| Solution membership | **10/10** `.csproj` present | A11 / A88 / B09 |
| Live `NewOrderSingle` | **OFF** (safe by absence, not by a proven flag) | A08 / A49 / A101 |

**Do not claim a trading platform.** Domain algorithms and some Application ports exist. Hosts, EF migrations, FIX sessions, workers, dashboard API, and tests required by §60 are not a first useful version.

### Binding pins this wave must not be walked back

- Pepperstone / cTrader is an **execution venue**, not an LP (A87, A25).
- Generic FIX 4.4 dictionary is **insufficient** (A36).
- Persist `ClOrdID` before send; never retry unknown as `35=D` (A42).
- Discover tag 55; never hardcode (A86).
- Plan-group env is **not** the group-fetch filter (A39, A40).
- Volume wire scale is **10 000**, not hundredths (A81, B14).
- No Kafka / K8s / ClickHouse / LLM / DNN / RL (A80).
- No ML until Phase 6 (A52, A104).
- `REAL_COPY_EXECUTION_ENABLED=false` until A100 + A101 are all PASS.

### Stale A-reports (keep on disk; do not delete)

A01–A06, A09/A10, A11 extras, A19 “no `.gitignore`”, A62 “0 page files”, A65 compose MISSING — superseded by B01–B08, A89, A88/B09, A103, B22, B23, B37.

### Next

Continue B-band to close compile/gap review, then implement only from A30 increments with reviewer + test gates. Do not enable live FIX.

---

## 2026-08-18 — C56 Directory.Build.props measured

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | C56 |
| Purpose | Read `Directory.Build.props`. Record what it actually sets. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\C56_directory_build.md` |
| Product source modified | **No** |
| Path | `D:\Prop\Directory.Build.props` (not under `src/`) |
| SHA-256 | `5ACD33B0F8E1A8D2E66956EF2B04A11E321661A5E3297F8F5C13051345562DD0` (269 B, 9 lines, LF, matches C19/C28; == HEAD) |
| Verdict | **EXISTS_NEEDS_REFACTOR.** Imported by all ten product projects. Sets `LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=false`, `Deterministic=true`. No `TargetFramework`. A30 I0 warnings-as-errors **not met**. `Directory.Build.targets` / `Directory.Packages.props` / `global.json` **MISSING**. A11/A30 “MISSING” is stale; A102 plan is not applied. |

---

## 2026-08-18 — C51 ScaleIn long add-lower is averaging down

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | C51 |
| Purpose | Read `TradeReconstructor.ScaleIn` after the long/short averaging-down change. Confirm long add-lower is averaging down. |
| Artifact | `D:\Prop\reports\swarm\20260818\C51_avg_down.md` |
| Product source modified | **No** |
| SUT SHA-256 (WT) | `E20457B398DB6CCC5F78ADE295A340CBC0646F5668F9F79F6AFBCC09D35741DD` |
| Verdict | **CONFIRMED.** Working tree: LONG `price < EntryVwap`, SHORT `price > EntryVwap`, compared **before** VWAP update. `Scale_in_and_partial_close` (0.10 @ 2300 then 0.10 @ 2290) **Passed**. HEAD `6c41447` still inverted (`>` / `<`). Change is **uncommitted**. §60 averaging-down remains PARTIAL (one fused fact; no F07/F08 / add-in-profit / short cell). B08 / A89 G1 stale vs working tree. |

---

## 2026-08-18 — C36 EfDashboardQueries remaining query / perf

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | C36 |
| Purpose | Read `EfDashboardQueries`. Remaining query issues? |
| Artifact | `D:\Prop\reports\swarm\20260818\C36_query_perf.md` |
| Product source modified | **No** |
| SUT | `src/Infrastructure/Dashboard/EfDashboardQueries.cs` — 168 lines / 7407 bytes / SHA-256 `37A4DDD23305708566888F0BBE2A6CC5DE253FB7151BDEE848195DE715EF4ACE` |
| Verdict | **YES — remaining issues.** N+1 on `GetGroupsAsync` (and `GetBrokersAsync`); `GetTradersAsync` full-table + O(n²) account join + no page; `GetTraderAsync` reloads the leaderboard; overview `ToList` of all scores; `destination_quotes` / `shadow_orders` / reject feed have no supporting index. Demo seed hides cost. No `EXPLAIN`. 0 tests. **FAIL as a 5k dashboard read path.** |

---

## 2026-08-18 — C28 SignalR package vs mapped hub

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | C28 |
| Purpose | Confirm whether `apps/api` has a SignalR package and whether any hub is mapped |
| Artifact | `D:\Prop\reports\swarm\20260818\C28_signalr_gap.md` |
| Product source modified | **No** |
| Verdict | **Package YES / hub NO.** Direct `PackageReference` `Microsoft.AspNetCore.SignalR.Common` 8.0.4 (restored, unused). Zero `AddSignalR`, zero `MapHub`, no `Hub` type, no `Hubs/` folder. Binding `/hubs/ops` is **MISSING**. Web stub still targets `/hubs/dashboard` and swallows start failure. Workers correctly do not host SignalR. A97 §0 “weatherforecast host” is stale (C04 hashes still match). |

---

## 2026-08-18 — C27 Redis lease gap

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | C27 |
| Purpose | `StackExchange.Redis` referenced — do workers implement / use a lease? |
| Artifact | `D:\Prop\reports\swarm\20260818\C27_redis_gap.md` |
| Product source modified | **No** |
| Verdict | **Package present, lease absent.** Infrastructure pins `StackExchange.Redis` 2.8.0; DLL sits next to both worker exes. **0** `using StackExchange.Redis` / multiplexer / Lua. `FixSessionOwnership` is an unused process-local `ConcurrentDictionary`. Workers never acquire/renew/release. §28 dual-owner protection is **MISSING** (vacuous `SAFE_BY_ABSENCE` of a TRADE socket only). |

---

## 2026-08-18 — C10 Fake MT5 group-discovery verify

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | C10 |
| Purpose | Read `FakeMt5BrokerConnector.cs`. Is group discovery plan-filtered? |
| Artifact | `D:\Prop\reports\swarm\20260818\C10_fake_mt5_review.md` |
| Product source modified | **No** |
| Verdict | **PASS** — `GetGroupsAsync` returns seeded `_groups` with no `MT5_GROUP_*` / `PlanMapping` intersection. Unmapped `demo\Maxmaster` and `real\standard` remain. This is the required §7/§9 shape, not a missing filter. |
| Not claimed | Live Manager connector; A40 overlay table; A79 discovery unit tests; complete Manager-visible catalog |

---

## 2026-08-18 — C07 workers send-off review

| Item | Value |
|---|---|
| Agent | C07 |
| Artifact | `reports/swarm/20260818/C07_workers_review.md` |
| Question | mt5-worker + fix-worker Program/Worker — real send off? |
| Product source modified | **No** |
| Verdict | **YES — real send is OFF** (`SAFE_BY_ABSENCE`). No `35=D`, no MT5 `SendTrade`. `CTrader:RealCopyExecutionEnabled=true` only logs. Dashboard LoggedOn/Ready is forged. |

### B25 (2026-08-18)

`reports/swarm/20260818/B25_secrets_rescan.md` — 79 product C# + 6 source `appsettings*.json`. **No live passwords.** Empty `CTrader:Password` and empty `ConnectionStrings:TraderIntelligence` in `apps/api/appsettings.json`. Live FIX/MT5 identifiers now in `CTraderFixOptions` / `DemoSeeder` / API JSON. Product source not modified.

### B26 (2026-08-18)

`reports/swarm/20260818/B26_ef_config_break.md` — committed `BrokersConfiguration` + 4 siblings bind missing plural types (`Brokers`, `Mt5Groups`, `Mt5Accounts`, `Mt5Deals`, `Mt5Positions`). Files deleted in WT; HEAD `TraderDbContext` also references 19 missing `DbSet<T>` types and 15 never-created `*Configuration` classes. Product source not edited.

### B39 (2026-08-18)

`reports/swarm/20260818/B39_ml_status.md` — `Get-ChildItem -Force D:\Prop\services` = **0 children**. No `ml-service`, no product Python, no XGBoost. Phase 6 remains closed. C# `BaselineScorer` exists (not A22, not ML). `EfDashboardQueries` hard-codes `mlProbability=null`. Product source not modified.

### B36 (2026-08-18)

`reports/swarm/20260818/B36_risk_fixtures.md` — designed five risk fixture families (stale quote `RF-SQ`, stale signal `RF-SS`, kill switch `RF-KS`, reduce allowed `RF-RA`, real send blocked `RF-RB`). Dual `expect_stub` / `expect_law` lanes. Recording send probe required for `submit_new_count=0`. Product source not modified. No JSON/tests created. G12/G13/G16 remain FAIL.

### C06 (2026-08-18)

`reports/swarm/20260818/C06_dbcontext_review.md` — new `TraderDbContext` has **0 composite PKs** (20× `HasKey(Id)`). Compound identity = 7 unnamed unique indexes. §10 solid only on `mt5_accounts` + `mt5_deals`. `reconstructed_trades` 4-col index is **not unique**. `fix_sessions.Qualifier` is globally unique (wrong). 0 named `*_uk`, 0 compound FKs, 0 migrations. Product source not modified.

### C23 (2026-08-18)

`reports/swarm/20260818/C23_empty_trader.md` — Demo Achiever **10003** (`contest\yo-2step`) has **0 deals / 0 positions**. `DemoSeeder` + `BaselineScorer` persist **`INSUFFICIENT_DATA`** (`N=0`). Measured persist **10 / 90 / 40.00** (empty snapshot still takes SL-rate penalty; B12 `0/100/40` is stale). Leaderboard still emits `EarlyScore=40` (A92 L7 leak). Product source not modified. Eval: `_tmp_c23_empty/stdout.txt` `VERDICT=PASS_INSUFFICIENT_DATA`.

### C17 (2026-08-18)

`reports/swarm/20260818/C17_unit_coverage.md` — `tests/Unit` vs Architecture §60: **0/17 COVERED**, 13 PARTIAL, 4 MISSING (deal dedup, drawdown, MFE/MAE, copy-intent idempotency). Measured `dotnet test` **83** total / **60** pass / **1** fail / **22** skip. Red fact is `Allocation_scales_before_step` (test math: expects `0.10×0.10=0.10`, SUT `0.01`). B08 averaging-down FAIL is stale (SUT polarity fixed). Product source not modified.

### C37 (2026-08-18)

`reports/swarm/20260818/C37_live_copy_page.md` — Architecture §46 **Live Copy Portfolio**: **page missing, chrome not.** `/live` + `LiveCopyPage.tsx` (321 B, SHA `F85CF339…`) + sidebar **`Live`**. No `GET /api/v1/live/portfolio`, no hook, no DTO, no `destination_positions`. A63 parks the GET (out of v1). Do not recreate the file; do not enable send to fill it. Product source not modified.

### C29 (2026-08-18)

`reports/swarm/20260818/C29_migrations_gap.md` — **no** `Migrations/` directory in the product tree. Schema path is `EnsureCreatedAsync` on api + mt5-worker + fix-worker. Default provider is `UseInMemoryDatabase("trader-intelligence")` because connection strings are empty / absent. `UseNpgsql` has no `MigrationsAssembly` and no `Migrate()`. A30 **0/15**. §60 PostgreSQL-migrations tests **0**. Product source not modified.

### C42 (2026-08-18)

`reports/swarm/20260818/C42_honesty_no_live_mt5.md` — Live Achiever and StarwaveFX Manager/HTTP sessions are **NOT proven**. Sole `IMt5BrokerConnector` is `FakeMt5BrokerConnector`; `ConnectAsync` sets `_connected = true`; DI always registers `DemoBrokerFactory.CreateDefault()`. Seeded IPs `57.128.141.65` / `84.201.6.142` are catalog paint. Dashboard `Connected` is literal `true`. C++ `mt5-sdk` is preserved, not wired. A100 G01 remains **FAIL**. Product source not modified.

### C44 (2026-08-18)

`reports/swarm/20260818/C44_honesty_no_ml.md` — **ML is not built, correctly.** `Get-ChildItem -Force D:\Prop\services` = **0 children**. No `ml-service`, no product Python scorer, no XGBoost. Phase 6 remains closed (`ML_NOT_IN_USE`, not `ML_UNAVAILABLE`). C# `BaselineScorer` exists (not A22, not ML). `EfDashboardQueries` hard-codes `mlProbability=null`. Product source not modified.

### C50 (2026-08-18)

`reports/swarm/20260818/C50_http_file.md` — `apps/api/TraderIntelligence.Api.http` **needs update**. Weather/`:5160` leftover is **GONE** (193 B, SHA `2AEC0F4A…`, `@api=:5000`). Live coverage **7/15** maps; **0** `###` separators so the file is one malformed request. Do **not** paste B06 §5.3 `/api/v1` yet (404). Product source not modified.

### C39 (2026-08-18)

`reports/swarm/20260818/C39_models_page.md` — Architecture §46 **Models** page is **missing by design**. No `ModelsPage.tsx`, no `/models`, no `useModels`, no `GET /api/v1/models`. Phase 6 closed; A63 parks `/models` out of v1; A30/A57 allow omitting the nav. Scoring + `BaselineScorer` + `mlProbability=null` is the Phase 0–5 substitute. Do not create the page to “complete” §46. Product source not modified.

### C54 (2026-08-18)

`reports/swarm/20260818/C54_remaining_gaps.md` — Honest remaining gaps vs architecture **§69**. Accepted still **0/12**. Three venue gaps remain: **live MT5** (Fake only; C++ unused; health lie), **live QUOTE logon** (no QuickFIX/n; 15 s `ReadyForMarketData` stamp), **real shadow fills from dest quotes** (`ShadowCopyEngine` unused; seeded bid/ask with null instrument id). Demo reconstruct/score/rank does not flip items 1, 9, 11. Product source not modified.

### D12 (2026-08-18)

`reports/swarm/20260818/D12_scorer_review.md` — Re-read `BaselineScorer.cs` (SHA `ECA2EEE8…`, 8143 B). **No LIVE promotion:** `FromBaseline` reachable set is `{INSUFFICIENT_DATA, RISK_BLOCKED, SHADOW, WATCH, EARLY_SCORE}` only; `CanPromoteToLive(_) => false`; `AfterHighEarlyScore() => SHADOW`; persist copies `SuggestedState` (cannot be LIVE). Vacuous lock, not A22 R5-before-R6. Case B still `WATCH`; mild martingale still `SHADOW`. Product source not modified.

---

## 2026-08-18 — Wave 2 (report recensus + INDEX table)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Wave | **2** |
| Purpose | Recatalog every markdown file under `D:\Prop\reports\swarm\20260818\` into `INDEX.md` as a three-column table (filename, first heading, one-line summary). Wave 1 catalog was a filename list at **141+** while B/C were still landing. |
| Report dir | `D:\Prop\reports\swarm\20260818\` |
| Index | `D:\Prop\reports\INDEX.md` |
| Markdown report count | **236** (A **105** + B **41** + C **60** + D **30**) |
| Product source modified | **No** |

### Inventory (wave-2 snapshot)

| Band | Count | Notes |
|---|---:|---|
| A01–A105 | **105** | Consecutive; no missing A IDs |
| B01–B41 | **41** | Consecutive; Wave 1 B-gaps B11/B17/B28/B34–B36 are closed |
| C01–C60 | **60** | Consecutive; Wave 1 C-band “just started” is closed |
| D-series | **30** | D-band landing (D01–D32 + D35 measured in this catalog; other D IDs still arriving) |
| Scratch | `_tmp_b35_cv/`, `_tmp_b35_score/`, `_tmp_c23_empty/` | Throwaway compile trees, not reports |
| **Report total** | **236** | All `*.md` directly under `swarm/20260818/` |

### What wave 2 measured (do not greenwash)

1. **§69 first useful version** still **accepted 0/12** (A57, C13). Demo Fake+InMemory ingest is not the bar.
2. **§68 go-live** still **0 PASS / 19 FAIL** (A100, C14). Live `NewOrderSingle` stays **off** (C07 `SAFE_BY_ABSENCE`).
3. **§70 live FIX** still **0/14 FAIL** (A101). Live QUOTE/TRADE Logon is **not proven** (C43).
4. Live Achiever/StarwaveFX Manager sessions are **not proven** (C42). Official QuickFIX/n is **not referenced** (C19).
5. ML is **not built**, correctly (B39, C44). Models page missing **by design** (C39).
6. Domain compiles clean (B01). Volume default is **10 000** (B14, D14).

Do not claim a trading platform. Product source was not modified by this cataloger.

---

## 2026-08-18 — D81 LiveCopyPage stub recensus

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D81 |
| Purpose | Read `LiveCopyPage.tsx`. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D81_livepage.md` |
| Product source modified | **No** |
| SUT | `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` |
| SUT SHA-256 | `F85CF339AAD7B2A9F639DA83466DC7949EF765EB3321C985044044978010BC82` |
| Measure | 321 B, 8 lines, 13:20:38, **untracked** vs HEAD `398a142`; same SHA as C37/D08 |
| Verdict | Chrome `/live` + sidebar `Live` exist. A26 §6.10 book **MISSING**. No hook, no GET, no dest table. Flag is a JSX literal. |

---

## 2026-08-18 — D57 scorer MFE fabrication

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D57 |
| Purpose | Does `BaselineScorer` fabricate MFE? |
| Artifact | `D:\Prop\reports\swarm\20260818\D57_mfe.md` |
| Product source modified | **No** |
| SUT SHA-256 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` |
| Measure | `_tmp_d57_mfe/D57_measured.tsv` — 8 books, all `AvgMfe=NULL`; VWAP 2300/2301 vs 2000/3000 scores identical |
| Verdict | **NO fabrication.** Omit + `FeatureQuality.Unavailable`. Persist/API/web have no MFE columns. Not a PASS of “MFE when valid”; `MfeMaeCalculator` still MISSING. |

---

## 2026-08-18 — E028 `client.ts` baseURL 5000

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | E028 |
| Purpose | Confirm `apps/web/src/api/client.ts` `baseURL` is port 5000. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E028_client.md` |
| Product source modified | **No** |
| SUT | `D:\Prop\apps\web\src\api\client.ts` |
| SUT SHA-256 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` |
| Measure | 232 B, 9 lines, clean vs HEAD `398a142`. `VITE_API_URL` absent. Live Kestrel pid 54468 `:5000` `/health` 200. Worktree launchSettings `:5000`; HEAD still `:5160`. |
| Verdict | **CONFIRMED.** Fallback `http://localhost:5000` is the live axios base. Lab MATCH on worktree + running process. Not A26/A62 catalog client. `\|\|` empty-string trap latent. |

---

## 2026-08-18 — E031 live overview 2 SHADOW / 1 RISK_BLOCKED / 0 LIVE

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:26+05:30 / 13:50:49+05:30 |
| Agent | E031 |
| Purpose | Confirm API overview rollup. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E031_overview_live.md` |
| Product source modified | **No** |
| Query SHA-256 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` (unchanged vs D77) |
| Live `GET /api/overview` | HTTP 200: `shadow=2`, `riskBlocked=1`, `live=0`, `liveCandidates=0`, `watch=0`, `realCopyEnabled=false` |
| Cross-check | `/api/traders?state=SHADOW` → 10001+99001; `?state=RISK_BLOCKED` → 10002; `?state=LIVE` → `[]`; 10003 is `INSUFFICIENT_DATA` |
| Verdict | **Confirmed demo fixture, not a live desk.** `/api/v1/overview` still 404. `FromBaseline` cannot emit LIVE. Page still drops `live`. |

---
