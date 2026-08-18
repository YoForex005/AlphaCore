# D:\Prop\reports - Swarm Report Index

**Cataloged:** 2026-08-18 (wave 2 recensus)
**Snapshot time:** 2026-08-18 13:33:33 +05:30
**Method:** `Get-ChildItem *.md` of `D:\Prop\reports\swarm\20260818\` + first heading of each file
**Tree:** `D:\Prop\reports\swarm\20260818\`
**Markdown report count:** **236**
**Product source modified by this catalog:** **No**
**`D:\Prop\reports\agents\`:** empty (no per-agent copies)

| Band | On disk | Gaps |
|---|---:|---|
| A01-A105 | **105** | none |
| B-series | **41** | none in B01-B41 |
| C-series | **60** | none in C01-C60 |
| D-series | **30** | other D IDs still landing |
| E-series | **6+** | E001–E003, E008, E012, E018, **E025** landed; other E IDs still landing |
| Other bands | **6+** | E-band |
| **Report total** | **312+** | D/E-band still landing; E025 is nav × pages |

Scratch (not markdown reports): `_tmp_b35_cv/`, `_tmp_b35_score/`, `_tmp_c23_empty/` - throwaway compile trees.

**Wave-2 headline (do not greenwash):** first useful version **0/12** (A57/C13/**D41** current); sec68 go-live **0/19** (A100/C14); sec70 live FIX **0/14** (A101). Domain compiles clean (B01). Live `NewOrderSingle` stays **off** (C07, SAFE_BY_ABSENCE). Live MT5 and live FIX Logon are **not proven** (C42/C43). ML is **not built**, correctly (C44/B39).

**Stale-vs-later (use the later file):** A01->B01, A02->B02, A03->B03, A05->B05/C19/D05 (inventory: D05), A06->B06/C04/D06, A07/A08->B07/C07->**D07** (fix-worker now stamps Disconnected; B07/C07 Worker.cs hash stale), A09/A27 names->A89, A09 coverage->C17, D33 recon tests (5 facts)->**E018** (6 facts, SHA `CB223DDE…`), A11->A88/B09, A19->B25, A52/A104->B39/C44, A57->C13->**D41** (§69 0/12 re-score; C13 item-11 FAIL and forged LoggedOn are stale; D16 “zero callers” stale; D22 seeder LoggedOn stale — pin **E008**); A57 inventory cite forbidden → **D93**, A62->B22/C08/D08, A65->B37/C12/D63, A103/B40 gitignore->**D62** (A103 §6 still unapplied; `.env.example` deleted in worktree), A100->C14, B10 npm absence->C49, B10/B22 Live+Audit "never created"->C37/C38/C53, C08 page list->D08 (same 15 SHAs, remeasured 13:33:46), D38/C53 nav chrome->**E025** (14/14 nav×page join; Models/Login still absent), B29 types-vs-DTO->**D76** (live API + `JsonStringEnumConverter` + `TraderDetailDto`; B29 integer-wire / no-detail stale; D74 independently confirms string enums).

---

## Complete file table (`D:\Prop\reports\swarm\20260818`)

| filename | first heading | one-line summary |
|---|---|---|
| `E032_pages_200.md` | # E032 — Vite SPA routes return HTTP 200 (`apps/web` `:3000`) | Live Vite pid 49100: 16/16 `App.tsx` destinations GET+HEAD 200; identical 624 B shell SHA `26270EBB…`; `/login`/`/api/*` also 200 HTML; only `/favicon.ico` 404. Not page completeness. Product source not edited. |
| `E038_flag_api.md` | # E038 — Settings flag API: `featureFlags.REAL_COPY_EXECUTION_ENABLED=false` | Live GET /api/settings 200 returns hardcoded REAL_COPY=false; PUT/PATCH 405; /api/v1/settings* 404; LiveCopyEnabled is a different unmapped name. |
| `E025_nav.md` | # E025 — `DashboardLayout` nav vs `pages/` | 14/14 sidebar `to`s hit a routed page; 15 pages (Trader Detail not a leaf); Models/Login absent; 7/14 labels abbreviated; `/groups` ≠ `/mt5-groups`; HEAD is 12-item nav + empty `pages/`. |
| `E018_recon_cov.md` | # E018 — `TradeReconstructionTests` inventory and coverage | 6/6 smoke facts (SHA `CB223DDE…`); 0/25 A21 bit-for-bit; F17 cousin only; 1/22 A89 classes; §60 recon cluster PARTIAL. Supersedes D33 count. |
| `E003_route_matrix.md` | # E003 — React route × API endpoint matrix (`apps/web` × `apps/api`) | 16 React destinations vs 15 live unversioned maps; 11/11 hook hits; 0 `/api/v1`; Shadow/Live/Audit have no fetch; hub and SettingsController unmapped. |
| `A01_domain_audit.md` | # A01 — Domain Layer Audit (`src/Domain`) | Domain was a Class1 scaffold; every secsec10/14/16/22/39/45 type MISSING. Stale - use B01. |
| `A02_application_audit.md` | # A02 — Application layer audit | Application was empty Class1; no ports or use-cases. Stale - use B02. |
| `A03_infrastructure_audit.md` | # A03 — `src/Infrastructure` audit vs architecture v2 §§11, 13, 44, 45 | Infrastructure FAIL/skeleton: EF/Npgsql/Redis packages only, no persistence. Stale - use B03. |
| `A04_mt5_csharp_vs_sdk.md` | # A04 — C# `src/Mt5` vs C++ `mt5-sdk` (Architecture §§6–12) | Maps C++ IMT5Client onto C# IMt5BrokerConnector; C# coverage was 0/41 at audit time. |
| `A05_fix_ctrader_audit.md` | # A05 — `src/Fix.CTrader` senior-engineer audit | Fix.CTrader FAIL/MISSING: no QuickFIX/n, no QUOTE/TRADE sessions. Later: B05/C19. |
| `A06_api_audit.md` | # A06 — `apps/api` audit vs architecture §§46–54, 55, 59 | apps/api was the weatherforecast template, not a dashboard BFF. Later: B06/C04. |
| `A07_mt5_worker_audit.md` | # A07 — `apps/mt5-worker` audit (Worker.cs template delay loop) | mt5-worker was a 1s log loop; Phase 1 ingestion had not started. Later: B07/C07. |
| `A08_fix_worker_audit.md` | # A08 — `apps/fix-worker` audit vs Architecture §§25–34, 41–43, 67 Phase 4 & 7 | fix-worker FAIL/scaffold; Phase 4/7 and flag gates unbuilt. Later: B07/C07. |
| `A09_unit_tests_audit.md` | # A09 — Unit tests audit (`tests/Unit` vs Architecture §60) | Unit tests FAIL; 0/17 sec60 areas (empty UnitTest1). Names later in A89; coverage in C17. |
| `A10_integration_tests_audit.md` | # A10 — Integration tests audit (`tests/Integration` vs Architecture §60 + §61) | Integration tests FAIL; 0/8 sec60 areas and no sec61 FIX harness. Later: B08/C16. |
| `A11_solution_coverage.md` | # A11 — `Mt5TraderIntelligence.sln` coverage vs on-disk projects | All 10 product .csproj files already in the .sln (membership PASS, not implementation). |
| `A12_imt5_client_map.md` | # A12 — `IMT5Client` interface map | Binding map of C++ IMT5Client (groups, deals, orders, positions, ticks, events). |
| `A13_mt5_types_map.md` | # A13 — `mt5_types.h` binding map (C# DTOs) | Binding C# DTO map from mt5_types.h; do not invent fields or rescale volumes. |
| `A14_mt5_manager_local.md` | # A14 — MT5Manager local transport | MT5Manager is the local Manager-API IMT5Client (native DLL, not HTTP). |
| `A15_mt5_pool_watchdog.md` | # A15 — MT5 pool + watchdog (pool size, acquire timeout, reconnect backoff) | MT5Pool (size 8, 5s acquire) vs MT5Watchdog (5->60s); pool-size env unused. |
| `A16_mt5_http_client.md` | # A16 — `MT5HttpClient` REST / SSE / timeout inventory | Inventory of MT5HttpClient REST/SSE/timeouts and remote IMT5Client + X-API-Key. |
| `A17_ticks_and_ledger.md` | # A17 — Source ticks vs ledger (Architecture §17: MFE/MAE needs ticks; do not fabricate) | Tick bridge + ledger FAIL for exact MFE/MAE; do not fabricate ticks. |
| `A18_mt5_sdk_tests.md` | # A18 — mt5-sdk tests vs live probes (C# must-not-break) | Pins C++ hermetic tests vs live probes as C# must-not-break contracts. |
| `A19_security_secrets_scan.md` | # A19 — Security / secrets scan | No live passwords found; architecture hosts/logins flagged. Rescan: B25. |
| `A20_table_catalog.md` | # A20 — Complete Database Table Catalog | Canonical PostgreSQL table catalog from secsec10-11/44-45; unify overlapping aliases. |
| `A21_reconstruction_spec.md` | # A21 — Deterministic MT5 Deal Reconstruction Spec | Binding deterministic XAUUSD deal-reconstruction contract (Phase 2; first-3). |
| `A22_scoring_spec.md` | # A22 — Deterministic Baseline Scoring Specification | Binding baseline.v1 formulas; trade #3 is first official score, never live promotion. |
| `A23_risk_engine_spec.md` | # A23 — Risk Engine Specification | Risk engine is the final authority between scoring and any execution. |
| `A24_shadow_copy_spec.md` | # A24 — Shadow Copy Specification | Shadow copy uses destination QUOTE only; OPEN/INCREASE vs REDUCE/CLOSE differ. |
| `A25_fix_session_spec.md` | # A25 — cTrader FIX Session Specification | Two independent QUOTE+TRADE TLS 4.4 sessions; venue is not an LP; not ready to Logon. |
| `A26_dashboard_api_spec.md` | # A26 — React Dashboard Pages, API Contracts, and RBAC | React pages, REST contracts, and RBAC from architecture secsec46-54. |
| `A27_test_inventory.md` | # A27 — Test Inventory (class names) | Required unit/integration/replay/FIX-harness test class inventory. |
| `A28_phases_gates.md` | # A28 — Phase 0–8 Checklist, Go-Live Gates, First Useful Version, What Not to Build | Phase 0-8 checklists, go-live gates, first useful version, what not to build. |
| `A29_gap_analysis.md` | # A29 — Gap analysis vs Architecture v2 | Repo is Phase 0 / early Domain vocabulary, not a trading platform; sec69 0/12. |
| `A30_implementation_sequence.md` | # A30 — Implementation sequence (§73.C) | sec73.C incremental files/modules/migrations for the first useful version. |
| `A31_ctrader_fix_overview.md` | # A31 — Official cTrader FIX API overview (QUOTE vs TRADE, TLS ports, messages) | Official cTrader FIX overview: QUOTE vs TRADE, TLS ports, message set. |
| `A32_ctrader_fix_specification.md` | # A32 — cTrader FIX specification extract | Official RoE extract: Comp/Sub IDs, Logon, NOS, ER, SecurityList, market data. |
| `A33_ctrader_fix_send_recv.md` | # A33 — cTrader FIX send/receive: sequence, resend, heartbeat, disconnect | Send/receive page is a TCP sample; sequence/resend/heartbeat live on sibling RoE pages. |
| `A34_ctrader_fix_faq.md` | # A34 — cTrader FIX FAQs: multiple connections, duplicate reports, instrument IDs vs tag 55 | Multiple connections copy reports; tag 55 is a numeric instrument ID, not a ticker. |
| `A35_quickfixn_packages.md` | # A35 — QuickFIX/n packages for .NET 8, FIX 4.4 dictionary, SSL, cTrader customization | Pin QuickFIXn.Core + QuickFIXn.FIX44 1.14.1; no FIX5/FIXT; no unofficial forks. |
| `A36_ctrader_data_dictionary.md` | # A36 — Is a Generic FIX 4.4 Data Dictionary Sufficient for cTrader? | Generic FIX 4.4 dictionary is insufficient; need FIX44-CSERVER.xml + tags 1000-1008. |
| `A37_mt5_deal_enums.md` | # A37 — MT5 Manager SDK deal enums (`EnDealAction`, `EnDealEntry`) and volume scale | Official EnDealAction / EnDealEntry and integer volume scale from Manager SDK headers. |
| `A38_mt5_volume_units.md` | # A38 — MT5 Manager API volume units (SDK vs `mt5_types.h`) | No MT5APIVolume type; official scales are MTAPI_VOLUME_* / VolumeExt. |
| `A39_mt5_group_discovery.md` | # A39 — Dynamically enumerate ALL groups for a manager login | Enumerate all manager-visible groups via GroupTotal/GroupNext; never filter by plan env. |
| `A40_plan_group_mapping.md` | # A40 — `plan_group_mappings` design (optional labels, never the fetch filter) | plan_group_mappings are optional labels, never the group-fetch filter. |
| `A41_outbox_design.md` | # A41 — PostgreSQL transactional outbox design | PostgreSQL transactional outbox (no Kafka); crash-safe ingest -> reconstruct -> score -> shadow. |
| `A42_clordid_idempotency.md` | # A42 — ClOrdID generation, persist-before-send, and `EXECUTION_STATE_UNKNOWN` | Persist unique ClOrdID before send; EXECUTION_STATE_UNKNOWN must not retry 35=D. |
| `A43_position_sizing.md` | # A43 — Source-to-destination quantity conversion (Architecture §38) | Binding source->destination quantity conversion for XAUUSD FIX tag 38. |
| `A44_symbol_normalization.md` | # A44 — CanonicalInstrument XAUUSD mappings | CanonicalInstrument XAUUSD mappings (aliases + numeric venue IDs). |
| `A45_mfe_mae_policy.md` | # A45 — MFE/MAE `feature_quality` policy (Architecture §17) | MFE/MAE feature_quality policy; never mix source ticks with destination quotes. |
| `A46_session_ownership.md` | # A46 — Single-active TRADE session ownership | Single-active TRADE ownership: Redis lease + fencing token; PostgreSQL remains authority. |
| `A47_reconciliation_design.md` | # A47 — cTrader startup and periodic reconciliation design | Startup + periodic cTrader reconciliation; TRADE is recon-only until Phase 8. |
| `A48_kill_switch.md` | # A48 — Kill switch design: `STOP_NEW_EXECUTION` vs `EMERGENCY_FLATTEN` | STOP_NEW_EXECUTION vs EMERGENCY_FLATTEN are distinct; never flatten source MT5. |
| `A49_feature_flags.md` | # A49 — Design flags `CTRADER_FIX_*` and `REAL_COPY_EXECUTION_ENABLED=false`: how workers enforce them | CTRADER_FIX_* may connect; REAL_COPY_EXECUTION_ENABLED=false is the only live-send license. |
| `A50_metrics_logging.md` | # A50 — Serilog enrichers, central redaction, OpenTelemetry metric names (§57–§58) | Binding Serilog enrichers / OTel names; logging+metrics MISSING; QuickFIX FileLog UNSAFE. |
| `A51_rbac_audit.md` | # A51 — Authentication, RBAC, and `audit_logs` Schema | Auth / four dashboard roles / audit_logs = MISSING; design only. |
| `A52_ml_not_yet.md` | # A52 — Why ML Is Not Built Now (Architecture §§19–21, Phase 6 Only) | Do not build ML now; Phase 6 only after the deterministic baseline is beaten OOS. |
| `A53_failure_rules.md` | # A53 — Failure Rules and No Blind Catch-Up | Fail-closed failure matrix; no blind catch-up; no invented trades or retry-of-unknown. |
| `A54_deployment_split.md` | # A54 — Deployment split: Windows `mt5-worker` + Linux API / Postgres / Redis / React | Windows mt5-worker + Linux API/Postgres/Redis/React; native SDK stays off Linux. |
| `A55_dead_code.md` | # A55 — Dead `dotnet new` template code | Live Class1/UnitTest1 gone at write; weatherforecast + worker 1s loops remained. Later: C15. |
| `A56_risk_list.md` | # A56 — Risk List (Architecture §73.D) | Phase 0 sec73.D risk register (SDK, Windows DLL, ticks, FIX headers, sizing, live safety). |
| `A57_first_useful_version.md` | # A57 — Architecture §69 first useful version (12 items) | **STALE inventory** (Class1 / weatherforecast / 0 pages / 0 tests). Accepted integer still 0/12. Current scorecard: D41. Stale pin: **D93**. |
| `A58_broker_registry.md` | # A58 — `IMt5BrokerConnector` + broker registry (Achiever + StarwaveFX) | One IMt5BrokerConnector + two configured instances (Achiever, StarwaveFX). |
| `A59_ingestion_checkpoints.md` | # A59 — Architecture §12: `sync_checkpoints`, historical backfill, live events, periodic reconciliation, idempotent upserts on `(broker_id, ticket)` | sec12 checkpoints/backfill/live/reconcile/(broker_id, ticket) upserts - design only. |
| `A60_correlation_phase2.md` | # A60 — Copy Correlation / Concentration (Architecture §65) — Phase 2 Hooks Only | sec65 copy-correlation hooks only; do not implement now. |
| `A61_efcore_schema.md` | # A61 — EF Core 8 + Npgsql Mappings (§45) | EF Core 8 fluent map of 43 sec45 tables; 0 migrations; replace incomplete TraderDbContext. |
| `A62_react_scaffold.md` | # A62 — React Dashboard Scaffold Plan (`apps/web`) | apps/web was a broken Vite stub (0 page files at write). Pages later exist - see B22/C08. |
| `A63_api_catalog.md` | # A63 — First Useful Version API Catalog (REST + SignalR) | First-useful REST + SignalR catalog; /weatherforecast is out of v1. |
| `A64_worker_pipelines.md` | # A64 — mt5-worker and fix-worker hosted service pipelines | Two authority-separated worker hosts; outbox consumers; fail-closed send; no god loop. |
| `A65_docker_compose.md` | # A65 — Docker Compose design (Postgres, Redis, API, web) | Compose design: I0 = Postgres+Redis; apps profile for API/web; mt5-worker stays off Compose. Superseded existence: B37/C12/**D63**. |
| `A66_docs_outline.md` | # A66 — Outline of `docs/*.md` that must be written (Architecture §66) | Outline of eleven sec66 docs/*.md files to write later; none authored here. |
| `A67_replay_harness.md` | # A67 — Architecture §60 Replay Harness Fixture JSON Format | Binding JSON fixture format for tests/Replay. |
| `A68_fix_simulator.md` | # A68 — Architecture §61: in-process FIX simulator | In-process sec61 cServer stand-in; does not authorize live NewOrderSingle. |
| `A69_trader_states.md` | # A69 — Trader State Transitions | Nine sec22 trader states; trade #3 + high score is SHADOW only. |
| `A70_execution_fsm.md` | # A70 — Destination-order FSM and duplicate ExecutionReport handling | Destination-order FSM + duplicate ExecutionReport handling per ClOrdID. |
| `A71_exposure_policy.md` | # A71 — OPEN_EXPOSURE / INCREASE_EXPOSURE / REDUCE_EXPOSURE / CLOSE_EXPOSURE policies | OPEN / INCREASE vs REDUCE / CLOSE exposure policies from sec64. |
| `A72_quote_guards.md` | # A72 — Configurable quote-age, spread, and price-move guards (Architecture §31, §37) | Configurable quote-age / spread / price-move guards on the destination QUOTE tape. |
| `A73_copy_latency.md` | # A73 — Copy latency: timestamps on every signal and metrics (Architecture §36) | Five sec36 timestamps + seven hop metrics; current pipeline is not timed. |
| `A74_source_dest_links.md` | # A74 — Persist source reconstructed trade → destination orders / positions (§35) | Persist reconstructed source trade -> dest orders -> dest position IDs (sec35); mapping MISSING. |
| `A75_env_example.md` | # A75 — Secret-safe `.env.example` (architecture §56) | Secret-safe .env.example (placeholders only); REAL_COPY_EXECUTION_ENABLED=false mandatory. |
| `A76_log_redaction.md` | # A76 — FIX / MT5 tags and config keys that must be redacted in logs | Binding denylist of FIX/MT5 tags and config keys; replace with literal ***. |
| `A77_health_ready.md` | # A77 — `/health` and `/ready` for API, MT5 worker, FIX worker | No process exposes /health or /ready; ready requires DB; FIX ready != real execution. |
| `A78_deal_idempotency.md` | # A78 — Unique `(broker_id, deal_ticket)` upsert, `ingestion_events`, duplicate metrics | Unique (broker_id, deal_ticket) upsert + ingestion_events; Phase 1 idempotency unproven. |
| `A79_fake_mt5_connector.md` | # A79 — `InMemoryMt5BrokerConnector` (test double) | InMemoryMt5BrokerConnector test double (groups/accounts/deals/events, including 5k sync). |
| `A80_not_to_build.md` | # A80 — What not to build (§71) | sec71 non-goals: no Kafka / K8s / ClickHouse / LLM / DNN / RL / mesh. |
| `A81_volume_unit_conflict.md` | # A81 — Volume unit conflict (`mt5_types.h` “hundredths” vs SDK `MTAPI_VOLUME_EXT_DIV`) | mt5_types.h "hundredths" comment is wrong; product transports 1 lot = 10 000. |
| `A82_deal_reasons.md` | # A82 — `IMTDeal::EnDealReason`: real trading vs ignore for reconstruction | EnDealReason buckets: real trading vs service money vs structural vs unknown. |
| `A83_canceled_deals.md` | # A83 — `DEAL_BUY_CANCELED` / `DEAL_SELL_CANCELED`: how reconstruction must treat them | DEAL_BUY_CANCELED / DEAL_SELL_CANCELED (13/14) are not fills; A21 addendum. |
| `A84_group_total_impl.md` | # A84 — Confirm `IMT5Client` `GroupTotal` / `GetAllGroups` / `GetGroupDetails` and manager implementation | Confirms GroupTotal / GetAllGroups / GetGroupDetails exist on C++ IMT5Client only. |
| `A85_yopips_extraction.md` | # A85 — YoPips extraction: preserve vs do not copy (payments / KYC) | Preserve YoPips MT5 read/subscribe; do not copy payments / KYC / email / dealer / challenge. |
| `A86_instrument_discovery.md` | # A86 — Instrument discovery: SecurityList request flow (never hardcode tag 55) | Discover venue instrument IDs via 35=x / 35=y; never hardcode tag 55. |
| `A87_not_an_lp.md` | # A87 — Do not call the cTrader account an LP | Pepperstone cTrader account is an execution venue, not an LP. |
| `A88_sln_plan.md` | # A88 — Plan: add all projects to `Mt5TraderIntelligence.sln` with nested `src` / `apps` / `tests` folders | Ten product .csproj files already nested under src/apps/tests; do not re-add. |
| `A89_unit_class_list.md` | # A89 — Complete xUnit class list | Authoritative xUnit class backlog (recon/score/risk/FIX/FSM/symbols); supersedes A09 names. |
| `A90_integration_class_list.md` | # A90 — Integration class list: Postgres migrations, outbox, restart backfill | Integration classes for Postgres migrations, MT5 backfill/restart, and outbox processing. |
| `A91_overview_dto.md` | # A91 — Overview page health tiles (§47) and required API DTOs | sec47 Overview tile grid + GET /api/v1/overview DTO; endpoint not implemented here. |
| `A92_leaderboard_dto.md` | # A92 — Trader Leaderboard query filters, sort, and JSON contract | Binding GET /api/v1/traders filters, sort grammar, and row DTO. |
| `A93_trader_detail_dto.md` | # A93 — Trader Detail DTO (Architecture §51) | Trader-detail wire contract including first-3 XAUUSD trades as a first-class block. |
| `A94_fix_page_dto.md` | # A94 — cTrader FIX Page DTO (Architecture §52) | sec52 FIX page allow-list DTO (QUOTE card vs TRADE card); password never on the wire. |
| `A95_risk_page_dto.md` | # A95 — Risk dashboard DTO including kill-switch state (Architecture §53) | sec53 Risk page DTO including complete kill-switch state (A48 semantics). |
| `A96_recon_page_dto.md` | # A96 — Architecture §54 Reconciliation Dashboard DTO | sec54 Reconciliation page allow-list DTO; comparer/repair stay in A47. |
| `A97_signalr_events.md` | # A97 — SignalR Hub Events: Live Scores, FIX Health, Quotes, Alerts | Ops hub events (scores/FIX health/quotes/alerts); API is the only hub host; MISSING. |
| `A98_pg_indexes.md` | # A98 — PostgreSQL indexes: 5,000 accounts, deals `(broker, login, time)`, reconstructed trades, outbox pending | Index contract for ~5k accounts, deals, reconstructed trades, outbox pending. |
| `A99_redis_keys.md` | # A99 — Redis key catalog (live scores, quote cache, session lease) | Allowed Redis keys (scores/quotes/leases); Redis is never SoT for orders/positions/balances. |
| `A100_golive_gates.md` | # A100 — Architecture §68 go-live gates (working checklist) | sec68 go-live checklist measured 0 PASS / 19 FAIL. Recheck: C14 still 0/19. |
| `A101_live_fix_acceptance.md` | # A101 — Architecture §70 live FIX acceptance (14 items, all FAIL) | sec70 live FIX acceptance scorecard: 0/14 FAIL; no real NewOrderSingle. |
| `A102_build_props.md` | # A102 — `Directory.Build.props` for net8 / nullable / implicit usings, plus extra NuGet pins | Directory.Build.props for net8 / nullable / implicit usings plus extra NuGet pins. |
| `A103_gitignore.md` | # A103 — `.gitignore` for `.env`, user-secrets, FIX store, logs | Recommended .gitignore for .env, user-secrets, FIX store, logs (does not rewrite the file). |
| `A104_ml_stub.md` | # A104 — `services/ml-service`: FastAPI Health Stub Only (No Training; Phase 6 Later) | If services/ml-service appears before Phase 6, it may be a FastAPI health stub only. |
| `A105_windows_dlls.md` | # A105 — Windows-only `mt5-worker` + copy-dlls from `vendor/Libs` | Windows-only mt5-worker must copy Manager DLLs from vendor/Libs; stays off Linux. |
| `B01_domain_compile_audit.md` | # B01 — Domain compile audit (`src/Domain`) | Domain compiles clean (0 errors / 0 warnings); Class1 gone; types now exist. |
| `B02_application_audit.md` | # B02 — `src/Application` audit vs architecture v2 §§6, 12, 32, 39 | Application FAIL/incomplete: sec6 ports exist; sec12 three-loop and sec32/sec39 orchestration missing. |
| `B03_infra_gap.md` | # B03 — Infrastructure gap vs architecture §45 (43 tables) | Infrastructure is a demo persistence slice, not the 43-table sec45 catalog. |
| `B04_mt5_gap.md` | # B04 — C# `src/Mt5` gap: `FakeMt5BrokerConnector` + registry + HTTP adapter | C# Mt5 has Fake connector + registry + HTTP adapter; live Manager path still missing. |
| `B05_fix_gap.md` | # B05 — Fix.CTrader gap: simulator + two session objects | Fix.CTrader gap: in-process simulator + two session objects; no live send authorized. |
| `B06_api_gap.md` | # B06 — `apps/api` weatherforecast leftovers and replacement endpoints | WeatherForecast C# gone from Program.cs; leftover launch/REST-client artifacts remained. |
| `B07_workers_gap.md` | # B07 — `apps/mt5-worker` and `apps/fix-worker` gap vs architecture / A64 | Workers are demo/fixture loops, not Phase 1/4 pipelines; LoggedOn is not a FIX session. |
| `B08_tests_gap.md` | # B08 — `tests/Unit` and `tests/Integration` gap | Unit/Integration gap vs sec60; a green dotnet test is not coverage. |
| `B09_sln_gap.md` | # B09 — `Mt5TraderIntelligence.sln` missing-project gap | No product .csproj missing from the solution; no dangling solution paths. |
| `B10_web_gap.md` | # B10 — `apps/web` existence check, gap analysis, and Vite React TS page plan | apps/web existence check, gap analysis, and Vite React TS page plan. |
| `B11_recon_review.md` | # B11 — Adversarial review: `TradeReconstructor` IN / OUT / INOUT / scale-in / partial / reverse | TradeReconstructor is a working happy-path netting book (scale-in, partial, reverse, INOUT). |
| `B12_scoring_review.md` | # B12 — BaselineScorer review (trade #3 SHADOW, leakage, formulas) | BaselineScorer review: trade #3 SHADOW, leakage checks, formula fidelity. |
| `B13_risk_review.md` | # B13 — RiskEngine review: kill switch, stale quote, REAL_COPY default, reduce vs open | RiskEngine is a fail-closed vocabulary stub; several assigned invariants would be unsafe live. |
| `B14_volume_review.md` | # B14 — VolumeConverter vs `mt5_types.h` vs `MT5APIMath.h` | VolumeConverter vs mt5_types.h vs MT5APIMath.h; wire scale is 10 000, not hundredths. |
| `B15_symbol_review.md` | # B15 — SymbolNormalizer review (aliases / venue IDs) | SymbolNormalizer: aliases and venue IDs must not be hardcoded (A16/A44/A86). |
| `B16_fix_fsm_review.md` | # B16 — ExecutionOrderStateMachine + ClOrdIdFactory: no-blind-retry review | ExecutionOrderStateMachine + ClOrdIdFactory: no-blind-retry on unknown states is implemented. |
| `B17_qty_review.md` | # B17 — QuantityNormalizer review | QuantityNormalizer review: last-stage step/min/max vs source-to-destination conversion. |
| `B18_shadow_review.md` | # B18 — ShadowCopyEngine destination-quote review | ShadowCopyEngine is a taker-touch calculator, not a destination-quote engine. |
| `B19_dbcontext_gap.md` | # B19 — `TraderDbContext` vs architecture §45 table gap | TraderDbContext: 18/43 sec45 tables as DbSet; 25 absent; no migrations or named UNIQUEs. |
| `B20_web_pages_gap.md` | # B20 — `apps/web` pages vs architecture §§46–54 | apps/web pages vs architecture secsec46-54: several required pages still missing or stubbed. |
| `B21_dbcontext_type_mismatch.md` | # B21 — `TraderDbContext` DbSet types vs `Domain\Entities` class names | No TraderDbContext DbSet type is missing from Domain/Entities class names. |
| `B22_web_missing_pages.md` | # B22 — `App.tsx` page imports vs `pages/` census | pages/ is not empty; A62 "13 imports / 0 page files" snapshot is stale. |
| `B23_template_leftovers.md` | # B23 — Template leftovers (`Class1`, `weatherforecast`) | Template leftover census (Class1, weatherforecast) after Domain/API growth. Later: C15. |
| `B24_connector_dup.md` | # B24 — Duplicate MT5 connector ports: `IMt5BrokerConnector` vs `IBrokerConnector` | Keep Application IMt5BrokerConnector; delete unused Mt5 IBrokerConnector duplicate. |
| `B25_secrets_rescan.md` | # B25 — Secrets rescan (new C# + appsettings) | Rescan of new C# + appsettings: no live passwords; empty CTrader:Password / connection string. |
| `B26_ef_config_break.md` | # B26 — EF `IEntityTypeConfiguration<T>` binds missing types | IEntityTypeConfiguration<T> files bind missing plural types; HEAD DbContext also broken. |
| `B27_cserver_case.md` | # B27 — `cServer` vs `CSERVER` (architecture §26) | BUG CONFIRMED in committed HEAD: TargetCompId CSERVER vs required cServer. |
| `B28_fix_parser_review.md` | # B28 — `FixMessageParser` + `FixSessionOwnership` review | FixMessageParser + FixSessionOwnership compile; neither is a production adapter or A46 lease. |
| `B29_dto_mismatch.md` | # B29 — TypeScript dashboard types vs `DashboardModels.cs` | TypeScript dashboard types and DashboardModels.cs disagree on almost every JSON key. |
| `B30_web_api_client.md` | # B30 — Web API client (`hooks.ts` + `signalr.ts`) | React hooks.ts + signalr.ts data-layer audit vs API contracts and hub path. |
| `B31_nav_gaps.md` | # B31 — Dashboard nav vs architecture §46 (Models, Live Copy, Audit) | Dashboard nav vs sec46: Models, Live Copy, and Audit gaps vs A26 routes. |
| `B32_ingestion_review.md` | # B32 — `DealIngestionService` group discovery is not filtered by plan mapping | DealIngestionService group discovery is not plan-filtered (correct sec7/sec9 shape). |
| `B33_entity_table_gap.md` | # B33 — Domain/Entities vs architecture §45 missing tables | Domain/Entities vs sec45: many required tables still have no entity type. |
| `B34_recon_fixtures.md` | # B34 — Eight concrete reconstruction deal fixtures (native scale 10 000) | Eight paste-ready reconstruction fixtures at native scale 10 000 (close/scale/partial/reverse). |
| `B35_score_fixtures.md` | # B35 — Scoring fixtures (N=2 insufficient; N=3 good → SHADOW; martingale → RISK_BLOCKED) | Scoring fixtures: N=2 insufficient; N=3 good -> SHADOW; martingale -> RISK_BLOCKED. |
| `B36_risk_fixtures.md` | # B36 — Risk fixtures: stale quote, stale signal, kill switch, reduce allowed, real send blocked | Binding risk fixtures: stale quote/signal, kill switch, reduce allowed, real send blocked. |
| `B37_docker_status.md` | # B37 — Docker Compose status (`D:\Prop`) | docker-compose.yml EXISTS (A65 MISSING claim is stale). |
| `B38_docs_status.md` | # B38 — `D:\Prop\docs` status vs architecture §66 | D:\Prop\docs is not empty and is not sec66-complete. |
| `B39_ml_status.md` | # B39 — ML status (`D:\Prop\services` empty; Phase 6 still closed) | D:\Prop\services is empty; no ml-service; Phase 6 still closed. |
| `B40_gitignore_env.md` | # B40 — `.gitignore` + `.env.example` (secrets not committed) | Confirms .gitignore + .env.example keep secrets uncommitted (read-only). |
| `B41_port_mismatch.md` | # B41 — API `launchSettings` vs web client port 5000 | Historically cited Kestrel :5160 vs web :5000 mismatch is CLOSED. |
| `C01_recon_tests_review.md` | # C01 — Trade reconstruction tests review (scale-in, partial, reverse, first-3) | Reconstruction tests are not sufficient for scale-in/partial/reverse/first-3. |
| `C02_score_tests_review.md` | # C02 — BaselineScorer unit-test review (no LIVE promotion) | BaselineScorer tests: no LIVE promotion confirmed; coverage still thin vs A22. |
| `C03_risk_tests_review.md` | # C03 — `RiskEngineTests` missing-case review | RiskEngineTests are thin smoke, not a risk-limits suite (FAIL vs assigned cases). |
| `C04_api_review.md` | # C04 — API host review: secrets to the browser? weatherforecast gone? | No sec55 secrets on live maps (safe by absence); weatherforecast gone from Program.cs. |
| `C05_di_review.md` | # C05 — Infrastructure `DependencyInjection` + `DemoSeeder`: circular-reference review | Infrastructure DI + DemoSeeder circular-reference review of composition root. |
| `C06_dbcontext_review.md` | # C06 — `TraderDbContext` compound-key review | TraderDbContext has 0 composite PKs; compound identity is unnamed unique indexes. |
| `C07_workers_review.md` | # C07 — mt5-worker / fix-worker Program + Worker: is real send off? | Real send is OFF (SAFE_BY_ABSENCE): no 35=D, no MT5 SendTrade; LoggedOn is forged. |
| `C08_web_pages_review.md` | # C08 — `apps/web/src/pages` census vs `App.tsx` imports | App.tsx imports match pages/ 15/15. |
| `C09_cserver_fixed.md` | # C09 — Is `TargetCompId` `cServer` now? | Worktree TargetCompId is cServer; committed HEAD is not. Not fixed as a committed fact. |
| `C10_fake_mt5_review.md` | # C10 — `FakeMt5BrokerConnector`: is group discovery plan-filtered? | FakeMt5BrokerConnector group discovery is not plan-filtered - required sec7/sec9 shape. |
| `C11_docs_gap.md` | # C11 — `D:\Prop\docs` vs architecture §66 required docs | D:\Prop\docs exists and is not empty; it does not satisfy architecture sec66. |
| `C12_compose_review.md` | # C12 — Docker Compose review (MT5 worker not forced onto Linux) | mt5-worker is not a Compose service and is not forced into a Linux container. Re-confirmed **D63**. |
| `C13_fuv_scorecard.md` | # C13 — Architecture §69 first useful version scorecard | sec69 first useful version still accepted 0/12; demo ingest path is not the bar. |
| `C14_golive_still_fail.md` | # C14 — Architecture §68 go-live gates: all still FAIL for live | All 19 sec68 go-live gates remain FAIL for live (same integer as A100). |
| `C15_leftovers.md` | # C15 — Leftovers (`weatherforecast`, `Class1`) in `apps` / `src` / `tests` | No Class1/weatherforecast in product C#; one leftover IIS Express launchUrl remains. |
| `C16_seed_test_review.md` | # C16 — `SeedingAndStoreTests` review (InMemory seed + deal upsert) | SeedingAndStoreTests are green InMemory smoke; they do not count as sec60 integration. |
| `C17_unit_coverage.md` | # C17 — `tests/Unit` vs Architecture §60 required unit tests | 0 of 17 sec60 unit areas COVERED (13 PARTIAL / 4 MISSING); A89 3/92 name-matches. |
| `C18_rbac_missing.md` | # C18 — Confirm RBAC is not implemented | RBAC is not implemented: no auth, roles, gates, or audit_logs writer. |
| `C19_quickfix_not_wired.md` | # C19 — QuickFIX/n package not referenced yet; simulator only | Official QuickFIX/n is not referenced; only an unwired in-process pipe simulator exists. |
| `C20_sdk_preserved.md` | # C20 — mt5-sdk C++ not deleted or rewritten | mt5-sdk C++ is preserved - not deleted and not rewritten. |
| `C21_cserver_grep.md` | # C21 — `CSERVER` grep under `D:\Prop\src` (intended: `cServer`) | Worktree uses cServer; HEAD still has CSERVER. Unstaged Fix.CTrader edits remain. |
| `C22_cors.md` | # C22 — `apps/api` CORS and Swagger (measured from `Program.cs`) | Worktree CORS is AllowAnyOrigin (UNSAFE); Swagger half-wired; HEAD still missing both. |
| `C23_empty_trader.md` | # C23 — Demo login 10003 (zero deals) scores `INSUFFICIENT_DATA` | Demo login 10003 (zero deals) scores INSUFFICIENT_DATA; scorer does not invent a book. |
| `C24_dev_ports.md` | # C24 — Vite `:3000` vs API `:5000` | Vite :3000 vs API :5000 is the intended two-process local split, not a port conflict. |
| `C25_serilog_gap.md` | # C25 — Serilog package is on the API; `Program` does not use it | API references Serilog.AspNetCore 8.0.2; Program.cs never uses it. Recensus: D54 (JSON now present, still unused). |
| `C26_otel_gap.md` | # C26 — OpenTelemetry not added (gap confirmation) | OpenTelemetry is not added to any product project (gap vs sec5/A50; compliant with A102 pin). |
| `C27_redis_gap.md` | # C27 — StackExchange.Redis referenced; workers use no lease | StackExchange.Redis 2.8.0 is referenced; workers acquire no session lease. |
| `C28_signalr_gap.md` | # C28 — API SignalR package present; no hub mapped | API references unused SignalR.Common 8.0.4 and maps no hub; client still dials /hubs/dashboard. |
| `C29_migrations_gap.md` | # C29 — EF migrations gap: no `Migrations/` folder; InMemory + `EnsureCreated` only | No EF Migrations/ folder; schema is InMemory + EnsureCreated only. |
| `C30_readme_gap.md` | # C30 — `D:\Prop\README.md` existence + landing-page gaps | D:\Prop\README.md exists (not missing); it is an incomplete landing page, not a create ticket. |
| `C31_recon_adversarial.md` | # C31 — Adversarial reconstruction: zero volume, canceled deals, mixed brokers | Zero-volume and canceled deals poison first-3 eligibility; mixed-broker isolation holds but is untested. |
| `C32_score_adversarial.md` | # C32 — Adversarial: can `EarlyQualityScore >= 70` with martingale? | Yes: implemented stub can score 70.25-85.25 SHADOW with FLAG_MARTINGALE; A22 baseline.v1 cannot. |
| `C33_risk_adversarial.md` | # C33 — RiskEngine adversarial: emergency flatten + close path | RiskEngine flatten/close is UNSAFE as a control and SAFE_BY_ABSENCE as a live send path. |
| `C34_api_usings.md` | # C34 — `apps/api/Program.cs` usings vs `ITradingStore` | API Program.cs already has the ITradingStore using; workers fully-qualify instead. |
| `C35_layering.md` | # C35 — Infrastructure references Mt5: first-useful-version layering | Infrastructure->Mt5 reference is acceptable for the demo slice, not the production topology. Recensus: D87 (edge SHAs unchanged). |
| `C36_query_perf.md` | # C36 — `EfDashboardQueries` remaining query / performance issues | EfDashboardQueries is a demo materializer: N+1, full-table loads, no pagination; UNSAFE at 5k accounts. |
| `C37_live_copy_page.md` | # C37 — Architecture §46 Live Copy Portfolio: missing? | sec46 Live Copy Portfolio page is missing; /live chrome + 8-line stub exist; no portfolio API. |
| `C38_audit_page.md` | # C38 — Architecture §46 Audit page | sec46 Audit leaf/nav exist; Audit page is a 324-byte stub with no GET /api/v1/audit. |
| `C39_models_page.md` | # C39 — Architecture §46 Models page is missing by design (Phase 6 closed) | sec46 Models page is missing by design while Phase 6 is closed; do not add it to complete nav. |
| `C40_index_html.md` | # C40 — `apps/web/index.html` root-div check | Product apps/web/index.html has <div id="root"></div>; path is not under src/. |
| `C41_report_count.md` | (NO HEADING) | Bare count file with no heading; body is a stale integer snapshot (158). |
| `C42_honesty_no_live_mt5.md` | # C42 — Honesty pin: live Achiever / StarwaveFX connections are NOT proven | Live Achiever/StarwaveFX connections are NOT proven; FakeMt5BrokerConnector only. |
| `C43_honesty_no_live_fix.md` | # C43 — Honesty pin: live cTrader FIX Logon is NOT proven | Live cTrader FIX Logon is NOT proven; LoggedOn rows and SimulateLogon are not a TLS session. |
| `C44_honesty_no_ml.md` | # C44 — Honesty: ML is not built (and that is correct) | ML is not built - correctly; services/ empty; Phase 6 closed; BaselineScorer is not ML. |
| `C45_readme_review.md` | # C45 — `D:\Prop\README.md` vs the repo | Root README is a lab stub whose paths exist; several sentences describe the goal as if running. |
| `C46_phase0_review.md` | # C46 — Independent review of `reports/PHASE0_AUDIT.md` vs repo | PHASE0_AUDIT.md is a rubber-stamp summary, not a measured Phase 0 audit (no hashes/quotes). |
| `C47_next_increment.md` | # C47 — Next increment: Windows live MT5 connect, QuickFIXn net8 QUOTE logon, EF migrations, RBAC | PLAN I-Live-Foundation: C47.1 migrations, C47.2 RBAC, C47.3 Windows collectors, C47.4 QuickFIXn 1.14.1 QUOTE Logon. Source not modified. |
| `C48_tailwind.md` | # C48 — `apps/web/tailwind.config.js` content globs | apps/web/tailwind.config.js content globs cover index.html + src/**/*.{js,ts,jsx,tsx}. |
| `C49_npm_status.md` | # C49 — `apps/web` npm lockfile and `node_modules` status | apps/web package-lock.json and node_modules both exist; B10/A65 absence claims are stale. |
| `C50_http_file.md` | # C50 — `TraderIntelligence.Api.http`: update needed? | TraderIntelligence.Api.http needs update: weather leftover gone, but 7/15 maps and no ### separators. |
| `C51_avg_down.md` | # C51 — TradeReconstructor `ScaleIn` after long/short averaging-down change | Working-tree ScaleIn long add-lower sets WasAveragedDown; HEAD still inverted; product source not edited. |
| `C52_expected_tests.md` | # C52 — Expected unit tests after avg-down polarity fix | Expect 29/29 on the B08 Unit census after avg-down polarity fix; that is not sec60 coverage. |
| `C53_nav_complete.md` | # C53 — LiveCopyPage + AuditPage existence (nav chrome) | LiveCopyPage + AuditPage exist and are routed; they are stubs, not complete sec46 pages. |
| `C54_remaining_gaps.md` | # C54 — Remaining gaps vs §69: live MT5, live QUOTE logon, real shadow fills | Remaining §69 blockers: live MT5, live QUOTE logon, and real shadow fills all still FAIL. |
| `C55_egress_ip.md` | # C55 — Achiever egress IP `81.29.145.69` is non-secret | Achiever egress IP 81.29.145.69 is NON-SECRET (documentable venue identifier). |
| `C56_directory_build.md` | # C56 — `Directory.Build.props` (measured; A30 I0 / A102 not applied) | Root props EXISTS (269 B, SHA `5ACD33B0…`); LangVersion=latest; warnings-as-errors false; no TFM/CPM/global.json. I0 incomplete. |
| `C57_sln_final.md` | # C57 — Final `Mt5TraderIntelligence.sln` membership (text parse, no `dotnet sln`) | Solution-membership PASS (text parse); not an implementation or go-live PASS. |
| `C58_outbox_dispatcher.md` | # C58 — Outbox entity exists; no dispatcher | OutboxEvent table is parked: entity exists, nothing inserts or drains rows. |
| `C59_copyintent_gap.md` | # C59 — Reconstruction does not emit `CopyIntent` | Confirmed: reconstruction emits no CopyIntent and no XAU_LIFECYCLE_* events. |
| `C60_ticks_missing.md` | # C60 — `mt5_xau_ticks` not in `TraderDbContext`; MFE unavailable | mt5_xau_ticks is MISSING from TraderDbContext; exact MFE is UNAVAILABLE. |
| `D01_domain_census.md` | # D01 — Domain census (`src/Domain`) | Domain has 47 authored .cs files, 59 public types, 10 namespaces; no root TraderIntelligence.Domain namespace. |
| `D02_application_census.md` | # D02 — Application layer census (`src/Application`) | Application product slice is 4 authored files / 9 145 bytes (contracts, dashboard models, ingestion). |
| `D03_infra_census.md` | # D03 — `src/Infrastructure` census (current tree) | Infrastructure census of the current persistence/DI slice (demo store, not §45 catalog). |
| `D04_mt5_census.md` | # D04 — C# `src/Mt5` census (`TraderIntelligence.Mt5`) | C# TraderIntelligence.Mt5 census: Fake connector / registry / HTTP adapter inventory. |
| `D05_fix_census.md` | # D05 — `src/Fix.CTrader` census (measured worktree) | 4 `.cs` / 7 public types / 0 QuickFIX/n / 0 session objects; types unused outside the assembly. |
| `D06_api_census.md` | # D06 — `apps/api` census (confirm no weatherforecast route) | 15 anonymous unversioned maps; **no** weatherforecast route/type/string in product source or Debug DLL. Not `/api/v1`. |
| `D07_workers_census.md` | # D07 — `mt5-worker` and `fix-worker` census | Two Worker SDK demo loops; 0/7 MT5 + 0/10 FIX A64 jobs; Fake ingest; FIX stamps Disconnected; send SAFE_BY_ABSENCE. |
| `D08_web_census.md` | # D08 — `apps/web/src` page census | 15 pages / 16 routes / 14 nav links; no Login; no Models; Groups is `/groups`; product source not modified. |
| `D09_tests_census.md` | # D09 — `tests/Unit` + `tests/Integration` census (test method names) | Census of tests/Unit + tests/Integration method names vs §60 coverage claims. |
| `D10_docs_census.md` | # D10 — `D:\Prop\docs` census + `reports/INDEX.md` inventory | D:\Prop\docs census plus reports/INDEX.md inventory vs architecture §66. |
| `D12_scorer_review.md` | # D12 — BaselineScorer review: no LIVE promotion | FromBaseline never emits LIVE/LIVE_CANDIDATE; CanPromoteToLive is hard-false; vacuous lock, not A22 R5. |
| `D13_risk_review.md` | # D13 — RiskEngine recensus: kill switch, stale quote, REAL_COPY, reduce vs open | RiskEngine recensus: kill switch, stale quote, REAL_COPY, reduce vs open. |
| `D14_volume.md` | # D14 — VolumeConverter default scale is 10 000 | Confirmed: VolumeConverter default scale is 10 000 (Manager classic), not 100 or 100 000 000. |
| `D15_symbols.md` | # D15 — `SymbolNormalizer` re-measure (aliases, venue IDs, persist gap) | Aliases are compiled-in; venue IDs not hardcoded here; mapper never reads source_symbol_mappings. |
| `D16_shadow.md` | # D16 — `ShadowCopyEngine` file review | ShadowCopyEngine file review: still a calculator, not a destination-quote engine. |
| `D17_exec_fsm.md` | # D17 — ExecutionOrderStateMachine + ClOrdIdFactory (measured close-read) | ExecutionOrderStateMachine + ClOrdIdFactory measured close-read (no-blind-retry). |
| `D18_qty.md` | # D18 — `QuantityNormalizer` re-measure (last-stage floor vs §38 converter) | EXISTS_NEEDS_REFACTOR as last-stage floor; MISSING as the §38/A43 converter; G7/G10 still FAIL. |
| `D19_dbcontext.md` | # D19 — `TraderDbContext` tables vs architecture §45 | Recensus: 18/43 §45 tables mapped by name; 25 missing; 2 extra; SHA `AFB195AC…`. |
| `D20_store.md` | # D20 — `EfTradingStore` idempotency (measured) | PARTIAL: same-context deal identity is first-write-wins; Phase-1 idempotency is not proven. |
| `D21_queries.md` | # D21 — `EfDashboardQueries` query catalog (what the dashboard actually reads) | EfDashboardQueries catalog of what the dashboard actually reads (demo materializer). |
| `D22_seeder.md` | # D22 — `DemoSeeder` writes `LoggedOn` without a FIX session | FORGED: DemoSeeder persists TRADE LoggedOn / QUOTE ReadyForMarketData with zero FIX. |
| `D23_di.md` | # D23 — `AddTraderIntelligence` composition-root inventory | AddTraderIntelligence composition-root inventory (what is actually registered). |
| `D24_fake.md` | # D24 — `FakeMt5BrokerConnector`: in-process demo book, not a broker | D24 — `FakeMt5BrokerConnector`: in-process demo book, not a broker |
| `D25_dup_iface.md` | # D25 — Duplicate collector ports: `IBrokerConnector` vs `IMt5BrokerConnector` | Keep Application IMt5BrokerConnector; delete unused Mt5 IBrokerConnector. B24 hashes unchanged. |
| `D26_cserver.md` | # D26 — Confirm `TargetCompId` default is `cServer` | Confirmed: session option defaults are ordinal cServer; assigning CSERVER is not folded. |
| `D33_recon_tests.md` | # D33 — `TradeReconstructionTests` coverage gaps | **STALE count** (5 facts / SHA `5D99BA22…`). Use **E018** (6 facts / SHA `CB223DDE…`). Gap catalog otherwise still holds. |
| `D101_recon_edges.md` | # D101 — Untested reconstruction edges: OUT_BY, zero volume, mixed broker | **0 product facts** in all 3 families. F09/F23/`RECON_ZERO_VOLUME` missing. Helper cannot express them. Z4/Z8 still first-3 poison. |
| `D38_routes.md` | # D38 — `App.tsx` + `DashboardLayout` route table | 16 destinations (index→/overview + 15 pages), 14 nav links; `/login` + `/models` absent; `/groups` ≠ A26 `/mt5-groups`; no catch-all; Live/Audit unstaged vs HEAD. |
| `D39_hooks.md` | # D39 — `hooks.ts` vs `Program.cs` endpoints | 11/11 demo GET hooks hit live `MapGet`s; 11/15 host maps have a hook; 0/11 `/api/v1`; trader detail is now `TraderDetailDto`. |
| `D28_harness.md` | # D28 — `FixSimulationHarness` review: **FLAG `55=123456`** | FixSimulationHarness review: FLAG 55=123456 hardcoded instrument id in the harness. |
| `D29_ownership.md` | # D29 — `FixSessionOwnership` is not §28 ownership | FixSessionOwnership is not architecture §28 TRADE ownership (no Redis lease). |
| `D31_mt5w.md` | # D31 — `apps/mt5-worker` Worker.cs + Program.cs (measured host) | D31 — `apps/mt5-worker` Worker.cs + Program.cs (measured host) |
| `D32_fixw.md` | # D32 — fix-worker `Worker.cs` does **not** stamp `LoggedOn` (no socket either) | fix-worker Worker.cs does not stamp LoggedOn; there is also no socket. |
| `D94_lie.md` | # D94 — Anti-evidence: “fix-worker stamps LoggedOn” | Assignment sentence FALSE today (Worker SHA `92A8F492…` stamps `Disconnected`). Mid-wave LoggedOn forge is gone and was anti-evidence of live FIX. |
| `E008_fix_status.md` | # E008 — DemoSeeder + fix-worker status: still forging `LoggedOn`? | **No.** Seeder `A6416491…` and Worker `92A8F492…` both persist `Disconnected`. No product `Status = LoggedOn` writer. Live Logon still unproven. |
| `D51_migrations.md` | # D51 — Migrations folder? Measured: **no** | No EF `Migrations/` on disk or in git; A30 0/15; hosts still `EnsureCreatedAsync`; C29 still holds. |
| `D35_risk_tests.md` | # D35 — `RiskEngineTests` re-read (5 green facts ≠ risk coverage) | RiskEngineTests: 5 green facts are not a risk-limits suite and do not satisfy §60. |
| `D41_fuv_now.md` | # D41 — Architecture §69 first useful version, scored against CURRENT repo | §69 **0/12 accepted**; DEMO 2/4–8/11; FAIL 1/3/9/10; React PARTIAL. A57 inventory stale (pin: D93). |
| `D48_shadow_rows.md` | # D48 — Are `ShadowOrders` created in the seeder? | YES as rebuild side-effect: 6 `shadow_orders` after `DemoSeeder` (10001+99001). Seeder file does not insert them. Not §24. |
| `D50_signalr.md` | # D50 — API `MapHub`: is a SignalR hub mapped? | **No.** Zero `MapHub`/`AddSignalR`; unused SignalR.Common 8.0.4; client still dials `/hubs/dashboard`. |
| `D53_rbac.md` | # D53 — Any auth on the API? **No.** | Dashboard API has **no** inbound auth/RBAC. 15 anonymous maps including `POST /api/ops/resync`. C18 verdict reconfirmed at Program SHA `61B1E0D1…`. |
| `D54_serilog.md` | # D54 — Serilog package used? | Package YES (`Serilog.AspNetCore` 8.0.2 on API); used NO (0 C# calls; unread JSON; workers none). |
| `D56_ticks.md` | # D56 — `mt5_xau_ticks` table: **MISSING**; exact MFE remains **UNAVAILABLE** | Table is architecture-only. 0 entity/DbSet/migration/SQL/writer. C60 SHA of `TraderDbContext` unchanged. |
| `D57_mfe.md` | # D57 — Does the scorer fabricate MFE? | **No.** `AverageMfe`/`AverageMae` stay null; `MaeMfeQuality=Unavailable`; VWAP mutation does not move scores. Omit, not EXACT. |
| `D62_gitignore.md` | # D62 — Root `.gitignore` recensus (measured) | Root ignore SHA `FAE817C1…` = HEAD; `.env` rules work. A103 §6 unapplied. Worktree deleted `.env.example` (bytes now ignored `.env`). Dirty API `fixstore`/`fixlogs` OPEN. |
| `D63_compose.md` | # D63 — `docker-compose.yml`: MT5 is not on Linux | **CONFIRMED:** compose lists postgres/redis/Linux api only; no mt5-worker; line-30 Windows comment. SHA `1ED8787F…` unchanged vs B37/C12. |
| `D79_fixpage.md` | # D79 — `FixSessionsPage.tsx`: is the password shown? | **No.** Disclaimer only; no password binding/input/JSON dump. DTO/entity have no password. SHA `EC933266…`. |
| `D93_a57_stale.md` | # D93 — A57’s 0/12 inventory is **STALE** (the §69 gate is not) | A57 empty-tree 0/12 is **STALE inventory**. Gate still **0/12** (D41). Do not cite A57 item table. SHA `278EF0B5…`. |
| `D72_first3.md` | # D72 — Is first-3 reconstructed completed XAU only? | Helper YES (completed XAU + eligible). Engine reconstructs all symbols. Increment not complete. Score/dashboard leak dirty XAU. |
| `D81_livepage.md` | # D81 — `LiveCopyPage.tsx` (measured stub, not the §46 book) | Untracked 321 B / 8-line stub SHA `F85CF339…`; `/live` chrome yes; A26 book no; unchanged vs C37. |
| `D87_layer.md` | # D87 — Infrastructure references Mt5: still OK? | **YES for Fake demo; NO as A54 topology.** Infra→Mt5 still `EXISTS_NEEDS_REFACTOR`. Persistence has 0 Mt5 usings. API/FIX-worker load `Mt5.dll` transitively. |
| `D75_launch.md` | # D75 — `launchSettings` weather leftover? | **No (worktree).** All 3 API `launchUrl` = `swagger`; SHA `BC022898…`. `HEAD` still 3× `weatherforecast` + `:5160`. |
| `D74_enums.md` | # D74 — Does the API use `JsonStringEnumConverter`? | **YES.** `ConfigureHttpJsonOptions` + default converter. Wire `"WATCH"` / `"Long"`, not ints. B10/B29 integer-wire stale. |
| `D77_overview.md` | # D77 — `OverviewPage.tsx` vs architecture §47 | File exists (untracked SHA `6497193F…`); **11/18** §47 tiles; `live`/`xauGross`/`xauNet` unpainted; MT5 OK is a catalog lie. |
| `D78_traders.md` | # D78 — `TradersPage.tsx` vs architecture §50 leaderboard | Demo 9-col table on `GET /api/traders`; **not** §50/A92. SHA `0AF0FF5B…`. Untracked. 4 §50 columns missing. |
| `D84_reconpage.md` | # D84 — `ReconciliationPage.tsx` vs architecture §54 | 12-line JSON dump of stub zeros+UtcNow. Chrome EXISTS_NEEDS_REFACTOR; §54/A96 MISSING; host map UNSAFE. SHA `BC036D09…` unchanged. |
| `D82_auditpage.md` | # D82 — `AuditPage.tsx`: chrome stub, not an audit reader | Untracked 324 B / 8-line stub SHA `8DE2F9B0…`; `/audit` chrome yes; A26/A63 reader no; C38 page bytes still hold. |

| `D97_nolive.md` | # D97 — Confirm `CanPromoteToLive` is **false** | **CONFIRMED.** Hard `=> false` at `BaselineScorer.cs:211` SHA `ECA2EEE8…`; no product callers; vacuous lock not A22 R5. |
| `D83_shadowpage.md` | # D83 — `ShadowPortfolioPage.tsx`: is it the §46 / A26 Shadow Portfolio? | **Chrome only.** 14-line stub SHA `608C8C2D…`; no hook/GET; 6 demo rows invisible; copy on approval/expiry is false. §69 in-v1 blocker. |
| `D96_id.md` | # D96 — Harness `123456` must not seed | **Must not seed; measured not seeded.** Harness L141 still FLAG. Seeder `VenueInstrumentId=null`. 0 hits in Infrastructure/apps. |
| `D76_types.md` | # D76 — `types/index.ts` vs live API (remeasure) | Unused stub (0 imports, SHA `B9CE20C1…`). 0/8 dashboard DTO pairs match. 4/13 TS types match anonymous health/recon/settings. B29 stale. |
| `D92_volume_vote.md` | # D92 — Vote: A81 default 1e8 vs B14 10 000 | **B14.** Ctor default stays 10 000 (`Volume()`). A81 1e8 is real ext scale, wrong default while extractors copy classic. |
| `E012_ports.md` | # E012 — API `:5000` / web `:3000` | Intended split: API 5000 + Vite 3000. Both listening this pass. Not a port bug. Product source not edited. |
| `E005_rules_matrix.md` | # E005 — Architecture risk / copy rules → RiskEngine + tests | **110** rules mapped. Engine SHA `AE0F9FAE…`. Tests SHA `7B952364…` (5 smoke facts). 18 MATCH / 22 PARTIAL / 11 STUB_WRONG / 41 MISSING. A89 #50–59 still phantom. Live copy SAFE_BY_ABSENCE. Product source not modified. |
| `E009_detail.md` | # E009 — `GetTraderDetailAsync` + `TraderDetailPage` vs architecture §51 / A93 | Demo header+table, not A93. Live 200 `null` miss; lowercase broker empties trades. Page 8 chips + First-3 column. 0/13 roots, ~2/16 §51, T9 only. Product source not modified. |
| `E031_overview_live.md` | # E031 — Live `GET /api/overview`: 2 SHADOW, 1 RISK_BLOCKED, 0 LIVE | Remeasured HTTP 200: `shadow=2`, `riskBlocked=1`, `live=0`. Traders 10001/99001 SHADOW, 10002 RISK_BLOCKED, 10003 leftover INSUFFICIENT_DATA. Not live venue. Product source not edited. |
| `E024_first3.md` | # E024 — Canceled position excluded from first-3? | **Helper YES / production NO.** Dirty scan excludes 13/14 `position_id` from helper (UNIT 2/false). Score/dashboard/persist leak (3/true/`SHADOW`). Official in-place unproven. C31/A83 stale. |
| `E028_client.md` | # E028 — `client.ts` `baseURL` is `http://localhost:5000` | Axios fallback `:5000` is live (`VITE_API_URL` unset). Worktree API MATCH; HEAD launchSettings still `:5160`. Not the A26 client. Product source not edited. |

**Counted markdown files:** 259 (E032 row added; not a full recensus)

