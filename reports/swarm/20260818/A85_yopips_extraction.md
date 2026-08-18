# A85 — YoPips extraction: preserve vs do not copy (payments / KYC)

| Field | Value |
|---|---|
| Agent | A85 (senior engineer, read-only of product source) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A85_yopips_extraction.md` |
| Pin | `D:\Prop\mt5-sdk\README.md` lines 1–8, 158–177 |
| Product | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` v2.0 §§6–12, §45, §71 |
| Product source modified | **No** |

This file is an extraction-boundary pin. It does not implement code. Later C# / C++ work must **subset** `mt5-sdk` as a source collector and must **not** re-import YoPips payments, KYC, email, challenge, or dealer product.

---

## 0. Verdict (measured)

`D:\Prop\mt5-sdk` is **not** a greenfield MT5 library. The README states it was **extracted from the YoPips prop-firm backend** so the same Manager plumbing can be dropped into other projects. That extraction is **partial and leaky**.

| Claim | Measured state |
|---|---|
| README: extracted from YoPips | **True.** First sentence of `D:\Prop\mt5-sdk\README.md`. Licence line: `src/`, `config/`, `tests/` are proprietary to **YoForex**. |
| README: originating `AppConfig` payment / KYC / email settings deliberately absent | **True as written, and confirmed on disk.** `AppConfig` + `.env.example` + `src/` have **zero** `STRIPE*`, `SUMSUB*`, `SMTP*`, `SENDGRID*`, `KYC_*`, `PAYMENT_*`, `MAIL_*`, `EMAIL_*` keys. |
| README: only MT5 plumbing remains | **False / incomplete.** `IMT5Client` still has `CreateUser` / `Deposit` / `Withdraw` / `SendTrade`. `MT5AccountHelper` still mints logins and maps `yo_pips_*` challenge plans. `MetricsService` still exports `propfirm_breaches_total` and a web-terminal quote hub. Ledger rows still have `user_id` / `challenge_id`. |
| This product is a YoPips clone | **No.** Architecture v2 is: read ~5,000+ MT5 accounts on Achiever + StarwaveFX → reconstruct / score XAUUSD → shadow-copy → cTrader FIX. It is **not** a challenge shop, PSP, or KYC desk. |
| Safe to copy the whole SDK onto `IMt5BrokerConnector` | **No.** That would put `Withdraw` next to history reads. |

**One-line law:** preserve the **read / subscribe / reconnect** MT5 layer; do **not** copy YoPips **payments, KYC, email, provisioning, dealer, challenge, or terminal** product.

---

## 1. What the README actually says

Opening:

> A reusable C++20 MetaTrader 5 integration layer, extracted from the YoPips prop-firm backend so the same MT5 plumbing can be dropped into other projects.

What it claims is *in* the extraction (`README.md` “What's in here”):

| Path | Role |
|---|---|
| `src/core/imt5_client.h` | transport-agnostic `IMT5Client` |
| `src/core/mt5_types.h` | DTOs |
| `src/core/mt5_manager.{h,cpp}` | local MetaQuotes Manager API |
| `src/core/mt5_pool.{h,cpp}` | bounded manager sessions |
| `src/core/mt5_http_client.{h,cpp}` | remote HTTP transport |
| `src/core/mt5_watchdog.{h,cpp}` | reconnect supervisor |
| `src/core/mt5_tick_bridge.{h,cpp}` | tick sink → subscriber fan-out |
| `src/core/chart_timeframe.{h,cpp}` | bar aggregation |
| `src/services/mt5_time_window.{h,cpp}` | server-time windows |
| `src/services/mt5_ledger_store.{h,cpp}` | optional Postgres ledger |
| `src/services/mt5_account_helper.{h,cpp}` | optional account helpers |
| `src/services/metrics_service.h` | latency / counters |
| `src/db/pg_pool.{h,cpp}` | optional libpq pool |
| `src/utils/` | UTF-8 ↔ wide, spdlog |
| `config/app_config.{h,cpp}` | `.env` + env loader, **“MT5 keys only”** |
| `tests/` | hermetic tests + two live probes |
| `vendor/MetaTrader5SDK/` | MetaQuotes Include / Libs / Docs / Examples |

Extraction notes (`README.md` 158–166) — three deliberate deltas from the originating backend:

1. **`AppConfig` is MT5 + Postgres + logging only.** Quote:

   > `config/app_config.h` carries only the MT5, Postgres and logging keys. The originating backend's `AppConfig` also held **payment, KYC and email settings**; those are **deliberately absent**.

2. Blank `MT5_SERVER_NAME` now falls back to the **endpoint host**, not a YoPips brand label. Set it explicitly for a broker-specific name.

3. `mt5_watchdog.h` includes `<nlohmann/json.hpp>` directly, **not** the backend’s Drogon-coupled JSON helper, so the core library stays free of Drogon.

Licence (`README.md` 170–177):

- `src/`, `config/`, `tests/` — proprietary to **YoForex**.
- `vendor/MetaTrader5SDK/` — MetaQuotes SDK, redistributed under the licence granted to the repository owner. **Not ours to sublicense.** Keep the repo private. Do not redistribute the vendor directory.

Honesty on the “Postgres keys” sentence: `AppConfig` as compiled (`app_config.h` / `app_config.cpp`) loads **MT5 + proxy + remote HTTP + `MT5_PASSWORD_ENCRYPTION_KEY` + `LOG_LEVEL` / `LOG_FORMAT`**. `DATABASE_URL` / `DB_*` appear only in `.env.example` as comments for a consumer built with `MT5SDK_WITH_POSTGRES=ON`. They are **not** members of `struct AppConfig`. The README overstates the Postgres coupling on the config struct. The payment / KYC / email absence claim is still accurate.

---

## 2. Two products, one leftover library

| | YoPips (origin) | Trader Intelligence (this repo) |
|---|---|---|
| Business | Prop-firm challenges: sell a plan, KYC the buyer, take a card/crypto payment, provision an MT5 login, deposit challenge balance, run pass/fail, email the trader, offer a web terminal | Identify high-quality XAUUSD traders on **existing** source accounts, shadow-copy, then route approved size to **cTrader FIX** |
| MT5 role | Admin + dealer + provisioner | **Source collector only** (Architecture §6) |
| Money movement | PSP + `DealerBalance` / `Deposit` / `Withdraw` on source MT5 | Destination is Pepperstone / cServer. Source MT5 must not be traded or funded by this software |
| Identity | `user_id` + `challenge_id` + minted login from `mt5_account_sequence` (starts 301100) | `broker_id` + login / ticket / position (§10). Logins already exist at the brokers |
| Groups | Plan → `demo\yo-2step` / `Flexy\yo-2step` for **CreateUser** | Discover **all** manager-visible groups; `MT5_GROUP_*` are optional labels only (§9) |
| Brokers | One `AppConfig` / one manager | Achiever + StarwaveFX as **two instances**, same connector type |
| Execution | `SendTrade` / `DealerSendOrder` on source | cTrader FIX 4.4 on destination. Source `SendTrade` is forbidden on the collector |

Copying YoPips product code into this tree would build the **wrong company**.

---

## 3. Three different “payments / KYC” surfaces (do not mix them)

The README’s “payment, KYC and email” sentence is about the **originating backend `AppConfig`**. The tree still contains two *other* payment/KYC-shaped surfaces. Treat them as three layers.

### 3.1 Layer A — YoPips backend AppConfig (PSP / KYC vendor / SMTP)

**Status: correctly not extracted. Do not re-add.**

Measured absence (workspace grep, 2026-08-18):

- `D:\Prop\mt5-sdk\config\` — no `STRIPE`, `SUMSUB`, `SMTP`, `SENDGRID`, `MAIL_`, `EMAIL_`, `KYC_`, `PAYMENT_`.
- `D:\Prop\mt5-sdk\src\` — same: **zero** hits.
- `D:\Prop\mt5-sdk\.env.example` — MT5 / proxy / remote HTTP / AES key / optional `DB_*` / `LOG_*` only.

The **original YoPips key names are not in this tree.** This report does **not** invent `STRIPE_SECRET_KEY` / `SUMSUB_TOKEN` / `SMTP_PASSWORD` as if they were found. The binding evidence is the README sentence plus the empty grep.

Do **not**:

- Add payment-processor, KYC-vendor, or transactional-email settings to `AppConfig`, C# `Mt5BrokerOptions`, worker `appsettings`, or dashboard Settings.
- Build checkout, invoices, refunds, AML review, document upload, or “resend verification email”.
- Store card PANs, bank accounts, government IDs, or KYC documents in §45 tables.
- Call Sumsub / World-Check / Espear / any PSP from `apps/mt5-worker` or `apps/api`.

### 3.2 Layer B — leftover dealer money movement on `IMT5Client`

**Status: extracted by accident (still on the interface). Do not copy onto the product collector.**

These are **not** a PSP. They are Manager-API balance operations that **credit or debit a live source trading account**. They exist because YoPips provisioned challenge balances.

```44:47:D:\Prop\mt5-sdk\src\core\imt5_client.h
    virtual bool DealerBalance(uint64_t login, double amount, const std::wstring& comment,
                               uint32_t type, uint64_t& dealId) = 0;
    virtual bool Deposit(uint64_t login, double amount, const std::wstring& comment, uint64_t& dealId) = 0;
    virtual bool Withdraw(uint64_t login, double amount, const std::wstring& comment, uint64_t& dealId) = 0;
```

Remote transport (YoPips-era microservice **not present in this repo** — A16 / A30):

```text
POST /mt5/accounts/{login}/balance
POST /mt5/accounts/{login}/deposit
POST /mt5/accounts/{login}/withdraw
```

(`mt5_http_client.cpp` 473–496.)

**Do not** put these on `IMt5BrokerConnector`. **Do not** implement the three POST routes in a C# “MT5 HTTP service”. If a later admin tool ever needs them, that is a separate `IMt5AdminClient` that the collector worker cannot reference (A04 §5.8).

**Do preserve** *reading* `DEAL_BALANCE` / credit / bonus / commission deals from `GetDeals`. Those rows are historical evidence that a deposit already happened. Observing them is not performing a payment.

### 3.3 Layer C — MetaQuotes vendor KYC / payments APIs

**Status: present only because the full vendor SDK is vendored. Do not wrap. Do not call.**

`vendor/MetaTrader5SDK/Include/` is a stock MetaQuotes drop. It contains:

| Header | What it is | Product action |
|---|---|---|
| `Config/MT5APIConfigKYC.h` | Broker-side KYC providers: `PROVIDER_KYC_SUMSUB`, `WORLD_CHECK`, `ESPEAR` | Do not wrap |
| `MT5APIManager.h` / `MT5APIServer.h` | `KYCCreate` / `KYCStart` / `KYCUpdate` / `KYCGet` | Do not call |
| `Bases/MT5APIClient.h` | `KYCStatus()`, `PersonAnnualDeposit()` | Do not read as a product KYC store |
| `Config/MT5APIConfigManager.h` | Rights `RIGHT_CFG_KYC`, `RIGHT_CFG_PAYMENTS`, `RIGHT_PAYMENTS_PROCESS`, `RIGHT_CLIENTS_KYC`, `RIGHT_PAYMENTS_*` | Do not grant / automate |
| `Config/MT5APIConfigAutomation.h` | Triggers `TRIGGER_KYC_*`, `TRIGGER_PAYMENT_*` | Do not subscribe |
| `MT5APIConstants.h` | `MT_RET_PAY_*` / `MT_RET_SUBS_PAYMENT_METHOD` | Ignore unless diagnosing a broker we do not drive |
| `Bases/MT5APIDocument.h` | `DOCUMENT_SUBTYPE_PAYMENT_METHOD` | Do not ingest ID docs |
| Examples `StopOutReporter` SMTP plugin params | Sample plugin, not ours | Do not copy |

`UserData.email` / `country` / `city` / `phone` on `mt5_types.h` are **MT5 user-record fields**, not an email gateway and not a KYC case file. Persist them on `mt5_accounts` only if §11/§45 require the column. Do **not** SMTP them. Do **not** treat `email` as “we run KYC.”

---

## 4. MUST PRESERVE (drop this into other projects — including this one)

Keep these as the reusable MT5 layer. Port behavior 1:1 into C#; do not rewrite the Manager API (A30).

### 4.1 Transports and supervision

| Item | Path | Why |
|---|---|---|
| Two-transport model | `IMT5Client` ← `MT5Manager` (local, WIN32) / `MT5HttpClient` (remote curl) | Architecture §6: one connector type, N broker instances. `MT5_MODE=local\|remote`. |
| `MT5Manager::Initialize` + `Connect` + `Disconnect` + `SetProxy` | `mt5_manager.{h,cpp}` | Connect is **not** on `IMT5Client`. C# `ConnectAsync` must wrap the concrete sequence. Default pump = users+orders+positions+symbols. No-pump fallback is a **real** path — treat as “events unavailable”, not “live”. |
| `MT5Pool` / `MT5Session` | `mt5_pool.{h,cpp}` | Request-only sessions so backfill does not own the pump mutex. Size to broker slot limit (`MT5_POOL_SIZE`). |
| `MT5Watchdog` | `mt5_watchdog.{h,cpp}` | Health + exponential reconnect. Required for live §12. |
| Proxy shape | `ProxyConfig`, `IS_MT5_PROXY_ENABLED`, `MT5_PROXY_*` | Achiever whitelist / StarwaveFX “design so proxy can be enabled later”. Never log proxy password. |
| Config resolution | process env → `.env` → built-in default | `AppConfig::load`. `.env` gitignored. |
| Generic `MT5_SERVER_NAME` fallback | `app_config.cpp` `endpointHost` | Do not hardcode a YoPips brand. Set explicitly per broker (`AchieverGlobalMarkets-Server`, StarwaveFX name). |
| CMake seams | `MT5SDK_WITH_POSTGRES=OFF`, `MT5SDK_WITH_DROGON=OFF` | Consumer pays only for what it uses. Local Manager sources compile on `WIN32` only. |
| `mt5sdk_copy_runtime_dlls` | CMake helper | Local mode loads MetaQuotes DLLs at runtime. |
| Vendor `Include/` + `Libs/` | `vendor/MetaTrader5SDK/` | Needed to compile/link Manager. Keep private. |

### 4.2 Read / subscribe contract (collector verbs)

Preserve signatures, fail-closed defaults, and comments. These are the subset Architecture §§6–12 needs.

| `IMT5Client` | Preserve because |
|---|---|
| `IsConnected` / `GetLastError` | Health, dashboard Brokers page, fail-closed when MT5 down |
| `GetEventQueue` | Live events. Adapt to `IAsyncEnumerable`. Pump/SSE thread: enqueue only |
| `GetUser` / `GetAccount` | `mt5_accounts` / `mt5_account_snapshots` |
| `GetUserLogins` / `GetGroupLogins` | Compose `GetAccountsAsync` |
| `GetPositions` | `mt5_positions_current` |
| `GetOrders` (default `false`) | Open/pending. Remote today unsupported — fail closed, do not fake |
| `GetDeals` **complete-history or `false`** | Callers must not make a pass/fail / score / copy decision on partial pages |
| `GetRecentDeals` | Pump ring; DealRequest can lag **>40 s**. Merge into `GetDealsAsync`. Empty+true ≠ “no history” |
| `GroupTotal` / `GetAllGroups` / `GetGroupDetails` | Dynamic discovery. **Not** `MT5_GROUP_*` |
| `SymbolTotal` / `GetSymbol` / `GetSymbolByName` / `GetManagerSymbols` / `GetGroupSymbols` | `mt5_symbol_metadata`. Empty+unsupported is failure, not “broker has no symbols” |
| `GetTickLast` / `SubscribeTicks` / `UnsubscribeTicks` | Ticks. OnTick = enqueue only. Remote SubscribeTicks default false → poll |
| `GetServerTime` | Checkpoints. Do **not** silently use host clock (`mt5_time_window` already flags fallback) |

### 4.3 DTO fields the collector must keep

From `mt5_types.h` (stamp `broker_id` at the C# boundary — C++ DTOs do not have it):

| Struct | Preserve |
|---|---|
| `GroupDetail` | name, currency, currency_digits, company, margin_call, margin_stop_out, connections_allowed |
| `UserData` | login, name, email, group, leverage, country, city, phone, registration, last_access, rights (PII columns, not a KYC workflow) |
| `AccountData` | login, balance, credit, equity, margin, margin_free, margin_level, profit, floating, storage |
| `PositionData` | ticket, login, symbol, action, volume (**hundredths of lots**), prices, SL/TP, profit, storage, times, comment |
| `DealData` | ticket, login, **order**, **position**, symbol, action, entry, volume, price, profit, commission, storage, time, comment |
| `OrderData` | ticket, login, symbol, type, state, volume, prices, time_setup, comment |
| `SymbolData` | symbol, path, description, digits, contract_size, volume_min/max/step, trade_mode |
| `TickData` | symbol, bid, ask, last, volume, time, time_msc, flags |
| `MT5Event` | type, login, variant payload |

Volume is native MT5 integer units. Do not treat as cTrader `OrderQty` (§1.10).

**Known defect to preserve-and-fix, not copy blindly:** `DealData.position` is extracted locally (`mt5_manager.cpp` `extractDeal`) but **omitted** from JSON `to_json`/`from_json`. Remote history drops the reconstruction key. C# local path must keep `position`. C# HTTP path must not trust current serde (A04, A29).

### 4.4 Behavioral contracts (do not “simplify”)

| Contract | Evidence |
|---|---|
| `GetDeals` follows every page/cursor or returns `false` | `imt5_client.h` 61–65. HTTP implements paging (max 10 000 requests). Local is a single `DealRequest` today — C# must be **at least** as strict as HTTP. |
| No `PUMP_MODE_DEALS` | MetaQuotes `MT5APIManager.h`. `OnDealAdd` is expected silent. Live deals = `GetDeals` poll + events if they fire. Do not assume `SubscribeAsync` is the deal path. |
| Pump vs pool split | Backfill on pool sessions. Never EF/Postgres from a P/Invoke / pump callback. |
| `CacheExecutedDeal` is **not** a collector API | It exists because YoPips **SendTrade** synthesizes the recent-deals ring. Source collector is not the dealer. |
| Group discovery ≠ plan map | §9 / A39 / A40. `GetAllGroups` is the fetch set. |
| Secrets | Never commit `.env`. Never log `MT5_PASSWORD`, `MT5_API_KEY`, `MT5_PASSWORD_ENCRYPTION_KEY`, proxy password, `UserParams.password`. |
| Hermetic tests | `tests/mt5_*_test.cpp` pin clamps, fail-closed remote calendar, ledger SHA-256, time-window fallback. Port assertions; do not hit live Achiever/StarwaveFX from unit tests. |
| Operator probes | `mt5_group_probe`, `mt5_news_calendar_probe` — opt-in, not CI. Group probe enumerates **all** manager-visible groups. |

### 4.5 Optional seams — keep the *idea*, not the YoPips schema

| Seam | Preserve | Do not copy as-is |
|---|---|---|
| Immutable raw-event + deal-revision **idea** | SHA-256 hex, never UPDATE evidence, new broker correction = new revision | Table names `mt5_raw_events` / `mt5_deals_ledger`; columns `server_key`, `user_id`, `challenge_id`. Product ledger is Architecture §45 EF (`ingestion_events`, `mt5_deals`, `broker_id`). A30: do not reuse this store. |
| Tick bridge **thread contract** | OnTick enqueue only; drain off pump thread; poll fallback if `SubscribeTicks` is false | Downstream `TerminalQuoteHub` / Drogon event loop / Redis fast outbox. That is the YoPips web terminal, not the React ops dashboard (A29 X06 DEPRECATED). |
| `PgPool` | libpq RAII pool pattern if a native helper stays | Not the v2 EF model. Not a second system of record. |
| `string_utils` UTF-8 ↔ wide | Required for Manager wide APIs | — |
| Logger **level/format** knobs | `LOG_LEVEL`, `LOG_FORMAT=json` | Filename `propfirm_backend.log` / `PROP_FIRM_LOG_DIR`. Product uses Serilog names from A50. |

---

## 5. MUST NOT COPY (payments, KYC, email, and the rest of YoPips product)

### 5.1 Explicitly called out by the README — payment / KYC / email AppConfig

Do not restore, port, or “complete” these into Trader Intelligence.

| Do not copy | Why |
|---|---|
| Payment-processor settings (card, crypto, wallets, webhooks, merchant IDs) | Originating `AppConfig` held them. Deliberately stripped. This product does not sell challenges and does not take deposits. |
| KYC-vendor settings (tokens, webhook secrets, applicant IDs, review queues) | Same. We do not onboard retail clients. |
| Email / SMTP / ESP settings (transactional mail, templates, “challenge passed” mailers) | Same. `UserData.email` is an MT5 field, not a mailer. |
| Any dashboard page for checkout, payouts, refunds, or identity review | Wrong product. Architecture dashboard is ops: brokers, traders, scores, shadow, risk, FIX. |

If a future human asks “YoPips had Stripe/Sumsub, should we add them for completeness?” the answer is **no**. Completeness for *this* product is Achiever + StarwaveFX **read** paths, not a payments stack.

### 5.2 Dealer / provisioning APIs still on `IMT5Client` — keep **off** `IMt5BrokerConnector`

Real methods. Extracted because YoPips created and funded accounts. Architecture §§6–12 is a source collector. Putting them on the C# connector invites mutating 5,000 trader accounts (A04).

| C++ | HTTP leftover (A16) | Why OUT |
|---|---|---|
| `CreateUser` | `POST /mt5/users` | Provisions a login. `UserParams` JSON includes **passwords**. |
| `DeleteUser` | `DELETE /mt5/users/{login}` | Destroys a source account. |
| `UpdateUser` / `UpdateUserGroup` | `PUT /mt5/users/{login}/group` | Moves live traders between groups. |
| `UpdateUserLeverage` | `PUT /mt5/users/{login}/leverage` | Changes risk on source. |
| `UpdateUserRights` | `PUT /mt5/users/{login}/rights` | Can disable trading / enable dealer bits. |
| `ChangePassword` / `CheckPassword` | `PUT .../password`, `POST .../check-password` | Secrets. Never needed to read history. Password in JSON body. |
| `DealerBalance` | `POST .../balance` | **Money movement.** |
| `Deposit` | `POST .../deposit` | **Money movement.** Closest leftover to “payments”. |
| `Withdraw` | `POST .../withdraw` | **Money movement.** |
| `DealerSendOrder` | `POST /mt5/dealer/order` | Places source orders. Destination is cTrader, not source MT5. |
| `SendTrade` | same dealer endpoint (market only) | Guarded YoPips execution. Default fail-closed is correct; do not implement on collector. |
| `CacheExecutedDeal` | n/a | Only valid after **our** `SendTrade`. |

A16 already listed the write routes. A30: **Do not call YoPips. Do not implement write/dealer routes.** Implement the **read + events** subset, plus the two remote gaps (`GetGroupDetails`, `GetOrders`).

### 5.3 Account helper = YoPips challenge shop

`src/services/mt5_account_helper.{h,cpp}` (`MT5SDK_WITH_POSTGRES`):

- Plan vocabulary: `yo_pips_1_step` / `yp_edge`, `yo_pips_2_step` / `yp_summit`, `yo_pips_instant` / `yp_instant`, `yp_core`, `yp_passfirst`.
- Phase 1–2 = demo/challenge, phase 3+ = funded. Instant skips demo.
- Compile-time defaults `demo\yo-2step`, `Flexy\yo-2step` — **drift** from Architecture §9 `contest\yo-2step` (A04, A40).
- `generateMt5Login` atomically updates `mt5_account_sequence` starting at **301100**.

**Do not** call this from the collector. **Do not** seed Achiever `demo\Maxmaster` with `yo-*` paths (A40). `MT5_GROUP_*` env names may be **preserved as optional labels** after discovery; they must **never** filter fetch. Do not put `getMt5Group` on `IMt5BrokerConnector`.

`MT5_DEFAULT_GROUP` / `MT5_PASSWORD_ENCRYPTION_KEY` are provisioning landing-group / at-rest password keys. Collector does not create accounts and should not encrypt investor passwords.

### 5.4 Ledger columns that are YoPips identity

`mt5_ledger::DealRevision` (`mt5_ledger_store.h`):

```text
userId
challengeId
serverKey          // string, not broker_id
accountRowId
```

SQL insert lists `user_id`, `challenge_id` (`mt5_ledger_store.cpp`).

**Do not** create those columns in the C# §45 schema. Preserve the **immutability rules** (SHA-256, no UPDATE, new revision on correction). Identity is `broker_id` + tickets.

### 5.5 Metrics, logs, and web terminal

`metrics_service.h` is a YoPips observability dump. Prometheus names are all `propfirm_*`.

Do **not** copy:

| Series / API | YoPips meaning |
|---|---|
| `propfirm_breaches_total` / `recordBreach` | Challenge failed |
| `propfirm_passes_total` / `recordPass` | Challenge passed |
| `propfirm_trade_blocked_daily_loss_total` / `_max_loss_total` | Prop-firm rule engine |
| `propfirm_deferred_violations_final_review_total` | Admin final review |
| `propfirm_terminal_*` / `incrementTerminalFeedClients` | Web-terminal WebSocket + Redis fast outbox |
| `propfirm_legacy_ws_clients` | Old YoPips WS |
| Trade-stage histogram (`Mt5DealerSend`, `PostCompliance`, `OutboxPublish`) | Source-side order.place path |
| `toJson()` dashboard blob for that product | Wrong dashboard |

**Do** preserve the *hygiene* rule already in the header: never put login / user / request id on a metric label.

Product metric names are frozen in Architecture §58 / A50. Do not prefix `ti_`. Do not copy `terminal_*`.

Logger (`src/utils/logger.h`): keep spdlog JSON/text idea; **do not** write `propfirm_backend.log` or honor `PROP_FIRM_LOG_DIR` in the C# hosts.

Tick bridge comments name `TerminalQuoteHub`. That sink is **not in this repo**. Do not rebuild a trader web terminal to “finish the extraction.”

### 5.6 Remote microservice that is not here

`MT5HttpClient` talks to a YoPips-era HTTP service (`MT5_REMOTE_URL`, default example `http://127.0.0.1:9100`). **No such server exists in `D:\Prop`.** A30: A16 is a *client* of a service that is not here.

Do **not**:

- Recreate the YoPips Drogon/HTTP dealer API so C# can `POST /deposit`.
- Ship `http://` as a production default (A29 UNSAFE).
- Invent `GET/POST /mt5/news` to make the calendar probe green (A18).

If v2 needs a native sidecar, it is a **read-only** Manager host exposing the A16 **GET / events** subset only.

### 5.7 Vendor Examples and broker KYC/payment plugins

Do not productize `vendor/MetaTrader5SDK/Examples/` (Gateway feeders, BalanceExample deposit UI, Web PHP registration + SMTP password samples, StopOutReporter SMTP). A19 already flagged sample `SMTP_PASSWORD='password'` in the PHP example. That is MetaQuotes sample code, not ours, and not a mailer to copy.

Do not wrap Manager `KYCStart` “because the header exists.”

### 5.8 Brand / filename leftovers

| Leftover | Action |
|---|---|
| Licence “proprietary to YoForex” | Keep the legal notice on the C++ tree. Do not pretend this SDK is Apache/MIT. Do not publish vendor. |
| `GroupDetail` comment `real\challenge_phase1_10k` | Comment only. Do not create that group. |
| Single-broker `AppConfig` | Do not add `MT5_STARWAVEFX_*` into this C++ struct as a second field set. Two **instances**. |
| `UserParams.to_json` password fields | Do not log. Do not send from C# collector. |

---

## 6. Full `IMT5Client` keep / drop card

Legend: **KEEP** = on `IMt5BrokerConnector` (or a thin sibling). **INTERNAL** = connector impl needs it, not a product verb. **LATER** = after Phase 1. **DROP** = YoPips admin/dealer/payments — not on the collector.

| Method | Class |
|---|---|
| `IsConnected`, `GetLastError` | KEEP |
| `MT5Manager::{Initialize,Connect,SetProxy,Disconnect}`, `MT5HttpClient::connect` | INTERNAL |
| `MT5Pool`, `MT5Watchdog` | INTERNAL |
| `GetEventQueue` | KEEP (`SubscribeAsync`) |
| `GetUser`, `GetAccount`, `GetUserLogins`, `GetGroupLogins` | KEEP |
| `CreateUser`, `DeleteUser`, `UpdateUser*` | **DROP** (provisioning) |
| `ChangePassword`, `CheckPassword` | **DROP** (secrets) |
| `DealerBalance`, `Deposit`, `Withdraw` | **DROP** (payments / money movement) |
| `GetPositions`, `GetOrders` | KEEP |
| `GetDeals`, `GetRecentDeals` | KEEP |
| `CacheExecutedDeal` | **DROP** (post-SendTrade) |
| `GetNewsCalendarItems`, `GetCalendarEvents` | LATER / do not fake remote |
| `SymbolTotal`, `GetSymbol`, `GetSymbolByName`, `GetTickLast` | KEEP |
| `GetAllTicksLast`, `GetManagerSymbols`, `GetGroupSymbols` | KEEP or LATER (fail-closed if empty+unsupported) |
| `SubscribeTicks`, `UnsubscribeTicks` | KEEP |
| `GetChart` | LATER (do not fabricate MFE/MAE from bars unless labeled) |
| `DealerSendOrder`, `SendTrade` | **DROP** (source execution) |
| `GroupTotal`, `GetAllGroups`, `GetGroupDetails` | KEEP |
| `GetServerTime` | KEEP (flag host fallback) |

A04 counted ~22 KEEP/INTERNAL, ~12 DROP, ~7 LATER of 41 methods. C# implements **0** today. That is the honest Phase-1 gap. Do not close it by copying DROP methods “so the port is complete.”

---

## 7. What a correct C# port looks like (boundary only)

```text
YoPips backend
  AppConfig: MT5 + Postgres + LOG + [payment + KYC + email]   ← payment/KYC/email stay in YoPips
  IMT5Client: read + dealer + provision
  AccountHelper: yo_pips_* plans, login mint
  Metrics: propfirm_* / terminal / challenge
  Ledger: challenge_id, user_id
        │
        │  extraction (README)
        ▼
mt5-sdk (this tree)     KEEP transports/DTOs/read contracts
                        STILL CONTAINS dealer + helper + propfirm metrics
        │
        │  this product may subset, not clone
        ▼
TraderIntelligence.Mt5  IMt5BrokerConnector = KEEP list only
  + broker_id stamp
  + two instances (Achiever, StarwaveFX)
  + §45 EF raw tables + outbox
  − no Deposit/Withdraw/CreateUser/SendTrade
  − no Stripe/Sumsub/SMTP
  − no challenge_id
  − no TerminalQuoteHub
```

Execution money, if any, is **cTrader FIX on the destination account**, behind shadow + risk + `REAL_COPY_EXECUTION_ENABLED` (A23 / A48 / A49). That is not YoPips `Deposit`.

---

## 8. Cross-checks (sibling swarm, not re-litigated)

| Report | What it already pinned |
|---|---|
| A04 | C++ is prop-firm admin+dealer; C# must subset; OUT table matches §5.2 here |
| A12 | Full `IMT5Client` map including `Deposit`/`Withdraw` |
| A16 | HTTP write routes `/deposit` `/withdraw` `/balance` `/dealer/order` |
| A17 | Tick bridge is a **terminal** sink; ledger is not §45 |
| A18 | “Do not drag payment/KYC/email into the MT5 worker config surface” |
| A19 | Vendor example SMTP passwords; no live PSP secrets in product configs |
| A29 | YoPips-shaped SDK = `EXISTS_NEEDS_REFACTOR`; terminal hub DEPRECATED |
| A30 | Reuse Manager client; do not reuse `challenge_id` / `user_id`; do not implement write routes |
| A39 / A40 | Discover all groups; do not seed Achiever with `yo-*` |
| A50 | Do not copy `propfirm_*` / `terminal_*` metric names |

---

## 9. Residual honesty

1. The originating YoPips `AppConfig` field list for payment / KYC / email is **not in this repository**. Absence is proven; the exact old key names are not. Do not invent them in C# “for compatibility.”
2. Extraction removed the **settings**, not all **behaviors**. Dealer cash, account minting, and challenge metrics remain. Treat those as **unfinished extraction**, not as product backlog.
3. MetaQuotes vendor KYC/payment headers will always be on disk if we vendor the Manager SDK. Presence of `MT5APIConfigKYC.h` is **not** authorization to start KYC.
4. `UserData.email` is easy to misread as “we have email.” It is a column on a user snapshot.
5. `DEAL_BALANCE` on a historical deal is easy to misread as “we have payments.” It is a read of something the **broker or YoPips** already posted.
6. This file does not authorize deleting C++ dealer methods. It forbids **copying** them onto the v2 collector and forbids **re-adding** Layer A settings.

---

## 10. Bottom line

| Question | Answer |
|---|---|
| What was extracted? | A reusable C++20 MT5 transport (local Manager + remote HTTP), pool, watchdog, types, optional ledger/helper, vendored MetaQuotes SDK. |
| What did the README refuse to copy? | Originating backend **payment, KYC, and email** `AppConfig`. Also: YoPips-branded server-name default; Drogon JSON helper on the watchdog. |
| What must this product preserve? | Transports, reconnect, proxy, group/account/deal/order/position/tick **reads**, event queue, complete-history `GetDeals`, no-pump honesty, UTF-8/wide, secret hygiene, fail-closed defaults. |
| What must not be copied? | PSP/KYC/SMTP config; `Deposit`/`Withdraw`/`DealerBalance`; `CreateUser` and password APIs; `SendTrade` on source; `yo_pips_*` login mint; `challenge_id`/`user_id` ledger; `propfirm_*` / terminal metrics; vendor Examples; Manager `KYC*` wrappers. |
| Is `mt5-sdk` already the v2 collector? | **No.** It is a capable single-broker YoPips Manager library with dangerous extra surface (A29). |

**Preserve the plumbing. Leave the shop.**
