# D89 — Markdown file count for `reports/swarm/20260818`

| Field | Value |
|---|---|
| Agent | D89 (count-only) |
| Date | 2026-08-18 |
| Assigned | Count `*.md` files in `reports/swarm/20260818`. Write this file. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D89_count.md` |
| Product source modified | **No.** Report only. `D:\Prop\src` was not edited. |
| Measured at (UTC) | 2026-08-18T08:13:51Z |
| Measured at (local) | 2026-08-18T13:43:51+05:30 |
| Command | PowerShell `Get-ChildItem -Path $root -Filter '*.md' -File` and `-Recurse` |

---

## 0. Verdict

| Scope | Count | Notes |
|---|---:|---|
| `*.md` in folder root (pre-write) | **272** | `Get-ChildItem -File` (no recurse) |
| `*.md` recursive (pre-write) | **272** | Same set. **0** `*.md` under subfolders |
| `D89_count.md` present at snapshot | **0** | This file did not exist at measure time |
| Expected after this write (no concurrent landings) | **273** | 272 + this file |
| Live recount after write (UTC 2026-08-18T08:15:21Z) | **278** | 272 + this file + 5 concurrent D-landings |

**Headline: 272 markdown files** were on disk under `D:\Prop\reports\swarm\20260818` immediately before this report was written. All of them sit in the folder root. Nested `_tmp_*` trees contain **zero** `*.md`.

A second `Get-ChildItem` after write measured **278**. The extra six names vs the 272 are: `D62_gitignore.md`, `D70_kill.md`, `D72_first3.md`, `D73_canceled.md`, `D79_fixpage.md`, and this `D89_count.md`.

C41 (`C41_report_count.md`, 4 bytes, body `158`) is a **stale earlier snapshot**. Do not treat 158 as current.

The D-band is still landing while this census ran (D41/D48/D68/D69/D71 appeared in the minutes before the snapshot). Concurrent writers after `08:13:51Z` can raise the live count above 273.

---

## 1. Method

| Step | Action |
|---|---|
| List | `Get-ChildItem -Path 'D:\Prop\reports\swarm\20260818' -Filter '*.md' -File` |
| Recurse check | Same with `-Recurse`; compare `DirectoryName` to root |
| Prefix | First letter run of each filename (`A`/`B`/`C`/`D`) |
| D gaps | Parse `^D(\d+)_`; missing ids in `1..max(D_max, 89)` |
| Exclude | Non-`.md` (`.cs`, `.csproj`, `.tsv`, `.txt`, `bin/`, `obj/`) |
| Not done | No product edit, no INDEX rewrite, no hash of every file |

Windows `-Filter '*.md'` is case-insensitive. Every match on disk uses lowercase `.md`.

---

## 2. Prefix breakdown (pre-write)

| Prefix | Count | Id range on disk | Notes |
|---|---:|---|---|
| A | 105 | A01–A105 | Complete. A100–A105 plus A01–A99. |
| B | 41 | B01–B41 | Complete. |
| C | 60 | C01–C60 | Complete. |
| D | 66 | D01–D61, D63, D67–D69, D71 | Incomplete. See gaps. |
| Other | 0 | — | No unprefixed / `INDEX`-style names in this folder |
| **Total** | **272** | | 105+41+60+66 |

### D-band ids present (66)

`1–61, 63, 67, 68, 69, 71`

### D-band ids missing at snapshot (23, span 1–89)

`62, 64, 65, 66, 70, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89`

`D89` is this report. It is **not** in the 272.

---

## 3. Size

| Metric | Value |
|---|---|
| Sum of `Length` (272 files) | **7,215,063** bytes |
| Prior count artifact | `C41_report_count.md` = 4 bytes (`158` + newline) |

---

## 4. Inventory (272 names, pre-write)

A01–A105 (105):

`A01_domain_audit.md` `A02_application_audit.md` `A03_infrastructure_audit.md` `A04_mt5_csharp_vs_sdk.md` `A05_fix_ctrader_audit.md` `A06_api_audit.md` `A07_mt5_worker_audit.md` `A08_fix_worker_audit.md` `A09_unit_tests_audit.md` `A10_integration_tests_audit.md` `A11_solution_coverage.md` `A12_imt5_client_map.md` `A13_mt5_types_map.md` `A14_mt5_manager_local.md` `A15_mt5_pool_watchdog.md` `A16_mt5_http_client.md` `A17_ticks_and_ledger.md` `A18_mt5_sdk_tests.md` `A19_security_secrets_scan.md` `A20_table_catalog.md` `A21_reconstruction_spec.md` `A22_scoring_spec.md` `A23_risk_engine_spec.md` `A24_shadow_copy_spec.md` `A25_fix_session_spec.md` `A26_dashboard_api_spec.md` `A27_test_inventory.md` `A28_phases_gates.md` `A29_gap_analysis.md` `A30_implementation_sequence.md` `A31_ctrader_fix_overview.md` `A32_ctrader_fix_specification.md` `A33_ctrader_fix_send_recv.md` `A34_ctrader_fix_faq.md` `A35_quickfixn_packages.md` `A36_ctrader_data_dictionary.md` `A37_mt5_deal_enums.md` `A38_mt5_volume_units.md` `A39_mt5_group_discovery.md` `A40_plan_group_mapping.md` `A41_outbox_design.md` `A42_clordid_idempotency.md` `A43_position_sizing.md` `A44_symbol_normalization.md` `A45_mfe_mae_policy.md` `A46_session_ownership.md` `A47_reconciliation_design.md` `A48_kill_switch.md` `A49_feature_flags.md` `A50_metrics_logging.md` `A51_rbac_audit.md` `A52_ml_not_yet.md` `A53_failure_rules.md` `A54_deployment_split.md` `A55_dead_code.md` `A56_risk_list.md` `A57_first_useful_version.md` `A58_broker_registry.md` `A59_ingestion_checkpoints.md` `A60_correlation_phase2.md` `A61_efcore_schema.md` `A62_react_scaffold.md` `A63_api_catalog.md` `A64_worker_pipelines.md` `A65_docker_compose.md` `A66_docs_outline.md` `A67_replay_harness.md` `A68_fix_simulator.md` `A69_trader_states.md` `A70_execution_fsm.md` `A71_exposure_policy.md` `A72_quote_guards.md` `A73_copy_latency.md` `A74_source_dest_links.md` `A75_env_example.md` `A76_log_redaction.md` `A77_health_ready.md` `A78_deal_idempotency.md` `A79_fake_mt5_connector.md` `A80_not_to_build.md` `A81_volume_unit_conflict.md` `A82_deal_reasons.md` `A83_canceled_deals.md` `A84_group_total_impl.md` `A85_yopips_extraction.md` `A86_instrument_discovery.md` `A87_not_an_lp.md` `A88_sln_plan.md` `A89_unit_class_list.md` `A90_integration_class_list.md` `A91_overview_dto.md` `A92_leaderboard_dto.md` `A93_trader_detail_dto.md` `A94_fix_page_dto.md` `A95_risk_page_dto.md` `A96_recon_page_dto.md` `A97_signalr_events.md` `A98_pg_indexes.md` `A99_redis_keys.md` `A100_golive_gates.md` `A101_live_fix_acceptance.md` `A102_build_props.md` `A103_gitignore.md` `A104_ml_stub.md` `A105_windows_dlls.md`

B01–B41 (41):

`B01_domain_compile_audit.md` `B02_application_audit.md` `B03_infra_gap.md` `B04_mt5_gap.md` `B05_fix_gap.md` `B06_api_gap.md` `B07_workers_gap.md` `B08_tests_gap.md` `B09_sln_gap.md` `B10_web_gap.md` `B11_recon_review.md` `B12_scoring_review.md` `B13_risk_review.md` `B14_volume_review.md` `B15_symbol_review.md` `B16_fix_fsm_review.md` `B17_qty_review.md` `B18_shadow_review.md` `B19_dbcontext_gap.md` `B20_web_pages_gap.md` `B21_dbcontext_type_mismatch.md` `B22_web_missing_pages.md` `B23_template_leftovers.md` `B24_connector_dup.md` `B25_secrets_rescan.md` `B26_ef_config_break.md` `B27_cserver_case.md` `B28_fix_parser_review.md` `B29_dto_mismatch.md` `B30_web_api_client.md` `B31_nav_gaps.md` `B32_ingestion_review.md` `B33_entity_table_gap.md` `B34_recon_fixtures.md` `B35_score_fixtures.md` `B36_risk_fixtures.md` `B37_docker_status.md` `B38_docs_status.md` `B39_ml_status.md` `B40_gitignore_env.md` `B41_port_mismatch.md`

C01–C60 (60):

`C01_recon_tests_review.md` `C02_score_tests_review.md` `C03_risk_tests_review.md` `C04_api_review.md` `C05_di_review.md` `C06_dbcontext_review.md` `C07_workers_review.md` `C08_web_pages_review.md` `C09_cserver_fixed.md` `C10_fake_mt5_review.md` `C11_docs_gap.md` `C12_compose_review.md` `C13_fuv_scorecard.md` `C14_golive_still_fail.md` `C15_leftovers.md` `C16_seed_test_review.md` `C17_unit_coverage.md` `C18_rbac_missing.md` `C19_quickfix_not_wired.md` `C20_sdk_preserved.md` `C21_cserver_grep.md` `C22_cors.md` `C23_empty_trader.md` `C24_dev_ports.md` `C25_serilog_gap.md` `C26_otel_gap.md` `C27_redis_gap.md` `C28_signalr_gap.md` `C29_migrations_gap.md` `C30_readme_gap.md` `C31_recon_adversarial.md` `C32_score_adversarial.md` `C33_risk_adversarial.md` `C34_api_usings.md` `C35_layering.md` `C36_query_perf.md` `C37_live_copy_page.md` `C38_audit_page.md` `C39_models_page.md` `C40_index_html.md` `C41_report_count.md` `C42_honesty_no_live_mt5.md` `C43_honesty_no_live_fix.md` `C44_honesty_no_ml.md` `C45_readme_review.md` `C46_phase0_review.md` `C47_next_increment.md` `C48_tailwind.md` `C49_npm_status.md` `C50_http_file.md` `C51_avg_down.md` `C52_expected_tests.md` `C53_nav_complete.md` `C54_remaining_gaps.md` `C55_egress_ip.md` `C56_directory_build.md` `C57_sln_final.md` `C58_outbox_dispatcher.md` `C59_copyintent_gap.md` `C60_ticks_missing.md`

D present (66):

`D01_domain_census.md` `D02_application_census.md` `D03_infra_census.md` `D04_mt5_census.md` `D05_fix_census.md` `D06_api_census.md` `D07_workers_census.md` `D08_web_census.md` `D09_tests_census.md` `D10_docs_census.md` `D11_recon_bugs.md` `D12_scorer_review.md` `D13_risk_review.md` `D14_volume.md` `D15_symbols.md` `D16_shadow.md` `D17_exec_fsm.md` `D18_qty.md` `D19_dbcontext.md` `D20_store.md` `D21_queries.md` `D22_seeder.md` `D23_di.md` `D24_fake.md` `D25_dup_iface.md` `D26_cserver.md` `D27_parser.md` `D28_harness.md` `D29_ownership.md` `D30_api.md` `D31_mt5w.md` `D32_fixw.md` `D33_recon_tests.md` `D34_score_tests.md` `D35_risk_tests.md` `D36_exec_tests.md` `D37_integ.md` `D38_routes.md` `D39_hooks.md` `D40_secrets.md` `D41_fuv_now.md` `D42_gates_now.md` `D43_s70.md` `D44_reason_gap.md` `D45_outbox.md` `D46_checkpoint.md` `D47_copyintent.md` `D48_shadow_rows.md` `D49_detail_thin.md` `D50_signalr.md` `D51_migrations.md` `D52_qfn.md` `D53_rbac.md` `D54_serilog.md` `D55_redis.md` `D56_ticks.md` `D57_mfe.md` `D58_lp.md` `D59_tmp_junk.md` `D60_sln.md` `D61_env.md` `D63_compose.md` `D67_http_groups.md` `D68_plan_filter.md` `D69_flag.md` `D71_expire.md`

---

## 5. Honesty

- Count is a **directory listing**, not an INDEX.md row count.
- Subfolder `_tmp_*` projects are **not** markdown and are **not** in 272.
- `C41_report_count.md` = **158** is obsolete.
- This file would have been the 273rd **if** nothing else landed between snapshot and write.
- Live post-write recount (2026-08-18T08:15:21Z): **278** = 272 + `D89_count.md` + five concurrent files (`D62_gitignore.md`, `D70_kill.md`, `D72_first3.md`, `D73_canceled.md`, `D79_fixpage.md`). D-band then present: 72 files. Still missing in 1–89: `64, 65, 66, 74, 75, 76, 77, 78, 80, 81, 82, 83, 84, 85, 86, 87, 88`.
- Later concurrent writers can raise the live count again. Re-run `Get-ChildItem -Filter '*.md' -File` for a later number.
- No product source was modified.
