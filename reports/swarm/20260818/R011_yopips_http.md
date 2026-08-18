# R011 — YoPips HTTP routes `/mt5/groups` and `/mt5/deals`

| Field | Value |
|---|---|
| Agent | R011 (senior engineer, confirm-only) |
| Date | 2026-08-18 |
| Assigned | Search `D:\Projects\YoPips` for HTTP routes `/mt5/groups` and `/mt5/deals`. Write this file. Do not copy secrets. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\R011_yopips_http.md` |
| Product source modified | **No.** Report only. |
| Secrets copied | **No.** Config *names* only. No API keys, tokens, passwords, or live URLs. |

**Tree searched:** `D:\Projects\YoPips` (Backend C++ PropFirm + frontend remaster).  
**Method:** literal and glob searches for `/mt5/groups`, `/mt5/deals`, `ADD_METHOD_TO`, swagger paths, frontend `src` callers. Minified `public/` bundles were **not** treated as route truth.

---

## 1. Verdict (measured)

| Question | Answer |
|---|---|
| Does YoPips Drogon expose inbound `GET /mt5/groups`? | **No.** |
| Does it expose inbound `GET /api/mt5/groups`? | **No.** |
| Does it expose inbound `GET /mt5/deals` or `GET /api/mt5/deals`? | **No.** Exact path does not exist. |
| Where does `/mt5/groups` appear? | **Outbound only.** Remote-mode `MT5HttpClient::GetAllGroups` calls `GET {MT5_REMOTE_URL}/mt5/groups`. |
| Where do deals go over HTTP? | **Not `/mt5/deals`.** Remote `GetDeals` calls `GET /mt5/accounts/{login}/deals?from=&to=` (plus cursor/page). |
| Is the remote microservice that *serves* those paths in this tree? | **No.** YoPips is the *client*. Server lives outside this repo (configured by `MT5_REMOTE_URL`). |
| What do browsers/admins actually call? | Product `/api/...` routes below. They go through `IMT5Client` (local SDK or remote HTTP). |

**One-liner:** `/mt5/groups` is a remote-bridge client path; `/mt5/deals` is not a YoPips route at all. Product traffic uses `/api/admin/mt5-groups`, `/api/admin/groups`, `/api/mt5/change-group`, and `/api/mt5/trades/{login}` (plus admin account-detail live deals).

---

## 2. Search evidence

Literal `/mt5/groups` (C++/headers/docs/tests, excluding minified public assets):

| File | Role |
|---|---|
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_http_client.cpp` | Only producer of the string. Three outbound GETs: `/mt5/groups/count`, `/mt5/groups`, `/mt5/groups/{group}/logins`. |

Literal `/mt5/deals`:

| File | Role |
|---|---|
| *(none)* | Zero matches in the YoPips tree. |

Closest deals HTTP path:

| File | Literal |
|---|---|
| `mt5_http_client.cpp` | `"/mt5/accounts/" + login + "/deals?from=" + from + "&to=" + to` |

Swagger (`swagger.json`, server base `/api`) has **no** `/mt5/groups` and **no** `/mt5/deals`. Related documented inbound paths: `/admin/mt5-groups`, `/admin/groups`, `/mt5/change-group`, `/mt5/trades/{login}`.

Frontend `client/src` and `admin-dashboard/src`: **zero** `/mt5/groups` or `/mt5/deals`.

---

## 3. Outbound remote-bridge catalog (not product HTTP)

`IMT5Client` is implemented by `MT5Manager` (local SDK) and `MT5HttpClient` (remote). Remote mode is selected in `main.cpp` when `config.mt5_mode == "remote"`. Client is constructed from `mt5_remote_url` + `mt5_api_key` (env: `MT5_REMOTE_URL`, `MT5_API_KEY`). Values not copied here.

Transport (`mt5_http_client.cpp`):

- URL = `baseUrl` (trailing slash stripped) + path as written.
- Header: `X-API-Key` (value not copied).
- REST also sends `Content-Type: application/json`.
- **No URL-encoding** of group names, symbols, or deal cursors.
- Success is `resp.value("success", false)` plus shape-specific fields.
- HTTP status ≥ 400 forces `success=false` after JSON parse.

### 3.1 Groups (outbound)

| Method | Path | C++ | Expected JSON |
|---|---|---|---|
| `GET` | `/mt5/groups/count` | `GroupTotal` | `count` → `uint32` (default 0) |
| `GET` | `/mt5/groups` | `GetAllGroups` | `success` + `groups` as `string[]` |
| `GET` | `/mt5/groups/{utf8(group)}/logins` | `GetGroupLogins` | `success` + `logins` as `uint64[]` |
| *(none)* | details | `GetGroupDetails` | **Hard stub:** always `return false`. No HTTP. |

```654:677:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_http_client.cpp
uint32_t MT5HttpClient::GroupTotal() {
    auto resp = httpGet("/mt5/groups/count");
    return resp.value("count", (uint32_t)0);
}

bool MT5HttpClient::GetAllGroups(std::vector<std::string>& groups) {
    auto resp = httpGet("/mt5/groups");
    if (!resp.value("success", false)) return false;
    groups = resp["groups"].get<std::vector<std::string>>();
    return true;
}

bool MT5HttpClient::GetGroupDetails(std::vector<GroupDetail>& groups) {
    // Not implemented for HTTP client proxy mode — always returns false.
    // Group detail queries require direct SDK access via MT5Manager.
    return false;
}

bool MT5HttpClient::GetGroupLogins(const std::wstring& group, std::vector<uint64_t>& logins) {
    auto resp = httpGet("/mt5/groups/" + wideToUtf8(group) + "/logins");
    if (!resp.value("success", false)) return false;
    logins = resp["logins"].get<std::vector<uint64_t>>();
    return true;
}
```

Related outbound group *write* (not a `/mt5/groups` path):

| Method | Path | C++ | Body |
|---|---|---|---|
| `PUT` | `/mt5/users/{login}/group` | `UpdateUser`, `UpdateUserGroup` | `{"group": <string>}` |

Local SDK path (no HTTP): `MT5Manager::GetAllGroups` / `GetGroupDetails` walk Manager `GroupTotal()` cache. `MT5Session` (pool) has names/count/logins only — no details.

### 3.2 Deals (outbound) — not `/mt5/deals`

```505:548:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_http_client.cpp
bool MT5HttpClient::GetDeals(uint64_t login, int64_t from, int64_t to, std::vector<DealData>& out) {
    const std::string base = "/mt5/accounts/" + std::to_string(login) + "/deals?from=" +
        std::to_string(from) + "&to=" + std::to_string(to);
    // pages via next_cursor / page+limit=1000; hard cap 10000 requests;
    // incomplete has_more without cursor => false (never accept partial history)
}
```

Accepted item arrays (first match wins): `data` if array; else `data.items`; else `data.deals`.

Pagination (any of):

- `next_cursor` on root, `pagination.next_cursor`, or `data.next_cursor`
- `has_more` on root / pagination / data
- `pagination.total_pages` + incrementing `page`

If `has_more` is true and no cursor: **return false**. Loop/corrupt cursor: **return false**.

`DealData` fields (`mt5_types.h`): `ticket`, `login`, `order`, `position`, `symbol`, `action`, `entry`, `volume`, `price`, `profit`, `commission`, `storage`, `time`, `comment`.

**Codec gap (measured):** `from_json` / `to_json` for `DealData` omit `position`. Local SDK `extractDeal` *does* set `deal->PositionID()`. Remote HTTP deals therefore deserialize `position=0`. Admin `dealToJson` and `TradeService::dealToJson` emit `position` / `position_id` from that field. Position-aware history dedup / hint matching is therefore **empty on remote mode**.

SSE (not REST deals): `GET /mt5/events/stream` can emit `DealAdd` / `DealUpdate` / `DealDelete` with `data` → `DealData` (same codec, same `position` omission).

Local SDK deals: `MT5Manager::GetDeals` → `DealRequest(login, from, to)` (no HTTP).

---

## 4. Inbound product routes that *cover* groups and deals

Swagger server URL is `/api`. Drogon registers the `/api` prefix. Auth: `AuthFilter` (user JWT; controller reads `X-User-Id`) or `AdminAuthFilter`.

### 4.1 Groups — inbound

| Method | Drogon path | Handler | Auth / permission | Purpose |
|---|---|---|---|---|
| `GET` | `/api/admin/mt5-groups` | `AdminMT5Controller::listMT5Groups` | Admin; `mt5_accounts.read` | Live MT5 group list from `IMT5Client` |
| `GET` | `/api/admin/groups` | `AdminGroupsController::listGroups` | Admin; unmatched GET → authenticated-only (`admin.authenticated_read`) | DB `mt5_group_mappings` |
| `POST` | `/api/admin/groups` | `createGroup` | `mt5_accounts.manage` | Upsert mapping |
| `PUT` | `/api/admin/groups/{id}` | `updateGroup` | `mt5_accounts.manage` | Update mapping |
| `DELETE` | `/api/admin/groups/{id}` | `deleteGroup` | `mt5_accounts.manage` | Delete mapping |
| `POST` | `/api/admin/groups/reload` | `reloadGroups` | `mt5_accounts.manage` | Rebuild runtime map |
| `PUT` | `/api/admin/mt5-accounts/{login}/group` | `AdminMT5Controller::updateGroup` | `mt5_accounts.manage` | Change one account’s broker group |
| `POST` | `/api/mt5/change-group` | `AccountController::changeGroup` | User `AuthFilter` + ownership | User changes own login’s group |

#### `GET /api/admin/mt5-groups`

1. If `!s_mt5 || !IsConnected()` → **503** `"MT5 not connected"`.
2. Prefer `GetGroupDetails`. On success (local SDK only):

```json
{ "success": true, "total": N, "groups": [
  { "name", "currency", "currency_digits", "company",
    "margin_call", "margin_stop_out", "connections_allowed" }
]}
```

3. Else `GetAllGroups`. On remote HTTP this is the **only** working path:

```json
{ "success": true, "total": N, "groups": [ { "name": "<string>" } ] }
```

4. Fetch fail → **500** `"Failed to fetch groups from MT5"`.

Swagger documents `groups` as `string[]`. Implementation returns **objects**. Frontend admin `src` has **no** caller of `/admin/mt5-groups`.

Remote consequence: `GetGroupDetails` is always false, so admin list is **names only**.

#### `GET /api/admin/groups` (DB mappings, not broker walk)

Query: `mt5_group_mappings` columns `id, challenge_type, phase, account_size, mt5_group, swap_free, is_active, created_at, updated_at`.

Response:

```json
{
  "success": true,
  "groups": [ /* row objects */ ],
  "runtime_source": "mt5_group_mappings.is_active=true",
  "fallback": {
    "policy": "configured_default_group_on_missing_exact_active_mapping",
    "enabled": <bool>,
    "group": "<default or empty>"
  }
}
```

Create body (required): `challenge_type`, positive `phase`, finite positive `account_size`, non-empty `mt5_group`. Optional `swap_free` (default false). Upsert key: `(challenge_type, phase, account_size, swap_free)`. Post-write reload; persist-then-reload-fail → **500** with `mapping_persisted: true`.

Frontend: `admin-dashboard/src/services/adminService.js` → `/admin/groups` CRUD + reload.

#### `PUT /api/admin/mt5-accounts/{login}/group`

Body: `{ "group": "<non-empty string>" }`.

Guards: parse login; MT5 connected; identity match (DB vs broker); row lock; reject if deletion in progress (`delete_claimed`, `reconcile_claimed`, `broker_deleted_reconcile`, `completed`); `UpdateUserGroup` then persist `accounts.mt5_group`.

Frontend: `mt5Service.changeGroup(login, group)`.

#### `POST /api/mt5/change-group`

Body: `{ "login": uint64, "group": "<non-empty>" }`. Ownership: `accounts.login + user_id`. Then `UpdateUserGroup` + `UPDATE accounts SET account_type = group`. Response `{success, message:"Group changed"}`. 400 / 404 / 500 as coded.

Frontend: `client/src/services/flexyService.jsx` calls `/mt5/change-group` and warns if unavailable.

### 4.2 Deals — inbound (no `/mt5/deals`)

| Method | Drogon path | How deals appear |
|---|---|---|
| `GET` | `/api/mt5/trades/{login}` | `TradeController::getHistory` → `TradeService::getTradeHistory` → `GetDeals` |
| `GET` | `/api/mt5/open-trades/{login}` | Positions, not deals |
| `GET` | `/api/mt5/trade-stats/{login}` | Stats from `GetDeals` |
| `GET` | `/api/admin/mt5-accounts/{login}` | Optional `live.deals` via `mt5_read::deals` |
| `GET` | `/api/journal/sync/{login}` | Incremental `GetDeals` into journal tables |
| `GET` | `/api/export/trades/{login}` | CSV of `GetDeals` |
| `GET` | `/api/analytics/equity-curve/{login}` (and related) | Builds curve from `GetDeals` |
| `GET` | `/api/user/data-export` | GDPR export includes `GetDeals` |
| `GET` | `/api/admin/v2/trades` | Admin trade list (ledger/read model; not the `/mt5/deals` path) |

There is **no** `ADD_METHOD_TO(..., "/api/mt5/deals")` and **no** `/api/mt5/accounts/{login}/deals` on the product server. The `/mt5/accounts/{login}/deals` shape exists only on the remote bridge.

#### `GET /api/mt5/trades/{login}`

- `AuthFilter` + ownership (`accounts` row for `X-User-Id`). Unowned → **404**.
- Query `from` / `to` as `YYYY-MM-DD` (`sscanf` + `mktime`; `to` set to 23:59:59 local). Empty → service default last 30 days.
- Fetch fail → **502**.
- Response (service): `{ success, trades: [...] }`. Each trade from `TradeService::dealToJson`:

  `ticket`, `order`, `position`, `deal_id`, `position_id`, `symbol`, `type` (`BUY`/`SELL`/…), `entry` (`IN`/`OUT`/…), `lot_size` (`volume/10000.0`), `open_price`, `close_price`, `profit`, `commission`, `swap`, `close_time`, `comment`, `provisional`.

- Balance ops (`action >= 2`) stripped. Ticket + position-aware provisional collapse applied.

Frontend: `flexyService.getTradeHistory` → `/mt5/trades/${login}`.

#### `GET /api/admin/mt5-accounts/{login}` live deals

Query:

| Param | Default | Range | Effect |
|---|---|---|---|
| `deals_days` | 30 | 1–365 | Window length |
| `deals_limit` | 50 | 1–200 | Max emitted deals (newest first) |
| `include_history` | true | truthy | If false, `availability.deals.status = not_requested` |

Shared live-read budget 2500 ms. `live.deals[]` uses admin `dealToJson` (numeric `action`/`entry`, includes `position`). Permission: `mt5_accounts.read`.

Frontend: `mt5Service.getAccount(login)` (no explicit deals params in source).

#### Journal sync `GET /api/journal/sync/{login}`

`GetDeals` on incremental window. Inserts closed exits only (`entry==1` and `action<=1`). MT5 down → `success:true`, `synced:0`, message `"MT5 unavailable — try again later"` (does not 502).

---

## 5. Related inbound `/api/mt5/*` (context only)

Registered on the product server (not the remote bridge):

| Method | Path |
|---|---|
| GET | `/api/mt5/test-connection` |
| GET | `/api/mt5/server-time` |
| GET | `/api/mt5/accounts` |
| POST | `/api/mt5/create-demo` |
| GET | `/api/mt5/account/{login}` |
| GET | `/api/mt5/account/{login}/credentials` |
| PUT | `/api/mt5/account/{login}/close` |
| PUT | `/api/mt5/account/{login}/reactivate` |
| PUT | `/api/mt5/account/{login}/visibility` |
| POST | `/api/mt5/disable-trading` |
| POST | `/api/mt5/enable-trading` |
| POST | `/api/mt5/change-group` |
| POST | `/api/mt5/deposit` |
| POST | `/api/mt5/withdraw` |
| GET | `/api/mt5/trades/{login}` |
| GET | `/api/mt5/open-trades/{login}` |
| GET | `/api/mt5/trade-stats/{login}` |
| POST | `/api/mt5/close-position` |
| POST | `/api/mt5/reset-password` |
| POST | `/api/mt5/accounts/{login}/orders` |
| PATCH/DELETE | `/api/mt5/accounts/{login}/orders/{orderId}` |
| PATCH | `/api/mt5/accounts/{login}/positions/{positionId}` |
| POST | `/api/mt5/accounts/{login}/positions/{positionId}/close` |
| GET | `/api/mt5/calendar` |

None of these is `/mt5/groups` or `/mt5/deals`.

---

## 6. Tests in this tree

| Test | What it does **not** do |
|---|---|
| `tests/mt5_http_client_pool_timeout_test.cpp` | Pool/timeout only. Does not assert `/mt5/groups` or deals paths. |
| `tests/mt5_time_window_test.cpp` | Fake `GetDeals` / `GetAllGroups` return false (compile stub). |
| `tests/mt5_group_probe.cpp` | Local `MT5Manager::GetAllGroups` probe. No HTTP `/mt5/groups`. |
| `tests/legacy_close_compatibility_test.cpp` | Fake `GetDeals`/`GetAllGroups` false. |

No YoPips test hits a live `/mt5/groups` or `/mt5/deals` HTTP server.

---

## 7. Source hashes (read-only)

| SHA-256 | Path |
|---|---|
| `BF92F869C66700EB8433A8A87D615B881460B620A60CBA3A0A41361D6485D5B0` | `...\src\core\mt5_http_client.cpp` |
| `38DBC7EE7E0C8EA637652272C6252626ED43E6B33AD8C39A7521F5BF04E98588` | `...\src\core\mt5_http_client.h` |
| `8157D2CF6D44922BFAB209C572E878C6F64E57D45140CDC28325B3463645AC66` | `...\src\core\imt5_client.h` |
| `3528684D015BCA769DC2434ED71AB6936AB0931F8507BFF6290287C7252E3209` | `...\src\http\controllers\admin\admin_mt5_controller.h` |
| `0080F661C8CAA6CE5D9F301E9C234124D225C243C133C0BF726DA7A5112BC037` | `...\src\http\controllers\admin\admin_groups_controller.h` |
| `DFE4127EBEB9164AC09BE0530B84D21C5BEFDFC875574E3DEF9A440C08143C42` | `...\src\http\controllers\trade_controller.h` |
| `469B55C8BD1AEB662353D05B7267E557AEED468C40B7A240D2D2EC13AC481C7C` | `...\src\http\controllers\account_controller.h` |

---

## 8. Implications for Prop (copy-trading / dashboard)

If Prop needs “list groups” or “list deals” HTTP:

1. **Do not** implement product-facing `/mt5/groups` or `/mt5/deals` to “match YoPips public API.” YoPips does not serve those.
2. **Do** treat `/mt5/groups` and `/mt5/accounts/{login}/deals` as **optional remote-bridge** contracts used only when `mt5_mode=remote`.
3. Local SDK mode never calls those URLs; groups/deals come from Manager `GroupTotal` / `DealRequest`.
4. If building a remote adapter, `GetGroupDetails` has no path today. `GET /mt5/groups` is names only.
5. If deserializing remote deals, parse `position` explicitly. Current YoPips `from_json` drops it.
6. Product-facing equivalents to copy (if matching YoPips UX): `GET /api/admin/mt5-groups`, `GET /api/mt5/trades/{login}`.

---

## 9. Honesty / not claimed

- Did not call any live MT5 or remote URL.
- Did not read `.env` or print `MT5_API_KEY` / `MT5_REMOTE_URL` values.
- Did not verify the remote microservice implements the client’s assumed JSON.
- Minified `client/public/rtx-desktop/assets/index-YOYlZxOA.js` produced noisy ripgrep hits on huge lines; frontend `src` has no those path literals.
- Product source was not modified.
