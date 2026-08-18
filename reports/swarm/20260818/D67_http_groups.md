# D67 — Confirm `MT5HttpClient::GetGroupDetails` is a hard-false stub

| Field | Value |
|---|---|
| Agent | D67 (senior engineer, confirm-only) |
| Date | 2026-08-18 |
| Assigned | Confirm `MT5HttpClient` `GetGroupDetails` stub. Write this file. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D67_http_groups.md` |
| Product source modified | **No.** Report only. |
| Measured | 2026-08-18 (source `LastWriteTimeUtc` 2026-08-18T07:02:57Z) |

**Sources (quoted, not modified):**

- `D:\Prop\mt5-sdk\src\core\mt5_http_client.cpp` (SHA-256 `5D4ED9AAC6D9662B0765507CD8429CAA6A56CB640CE74715E3F237AB2FF83AF6`)
- `D:\Prop\mt5-sdk\src\core\mt5_http_client.h` (SHA-256 `38DBC7EE7E0C8EA637652272C6252626ED43E6B33AD8C39A7521F5BF04E98588`)
- `D:\Prop\mt5-sdk\src\core\imt5_client.h` (SHA-256 `CB8D632BB94ADC1145C0343418788010E6FEDC6886979A59B34E6332B104C707`)
- `D:\Prop\mt5-sdk\src\core\mt5_types.h` (`GroupDetail`; SHA-256 `1D3BE309AC89141C82EFD8F775812913412B5AA293C9B300D948B65329A99C63`)
- Contrast: `mt5_manager.cpp` (real walk), `mt5_pool.h` (no details method), `tests/mt5_group_probe.cpp`, `tests/mt5_time_window_test.cpp`, `tests/mt5_http_client_pool_timeout_test.cpp`
- Prior reports (not re-executed): A13, A16, A18, A30, A39, A40, A84

C# under `D:\Prop\src` has **zero** `GetGroupDetails` / `GroupTotal` / `GetAllGroups` symbols. Those names live only on the C++ `IMT5Client` surface.

---

## 1. Verdict (measured)

| Question | Answer |
|---|---|
| Does `MT5HttpClient` declare `GetGroupDetails`? | **Yes.** Required override of a **pure virtual** on `IMT5Client`. Header line 72. |
| Is the body a real REST proxy? | **No.** Four lines: two comments + `return false;`. |
| Does it call `httpGet` / `httpPost` / `httpRequest` / curl / SSE? | **No.** Zero network. The method cannot succeed even if the remote microservice is up. |
| Is there a `/mt5/groups/details` (or any details) path in this client? | **No.** A30 proposed `GET /mt5/groups/details` as NEW. It is **not** in `mt5_http_client.cpp`. |
| Does the out-parameter get cleared or filled? | **No.** `groups` is unused. Leftover caller data is left in place. |
| Does a sibling group API work over HTTP? | **Yes, names/count/logins only.** `GroupTotal` → `GET /mt5/groups/count`. `GetAllGroups` → `GET /mt5/groups`. `GetGroupLogins` → `GET /mt5/groups/{utf8}/logins`. |
| Can remote mode populate `GroupDetail`? | **No.** Only `MT5Manager::GetGroupDetails` fills the struct (local Manager cache walk). |
| Does `MT5Session` (pool) implement details? | **No.** Pool header has `GroupTotal` + `GetAllGroups` + `GetGroupLogins` only. |
| Is there a nlohmann `to_json`/`from_json` for `GroupDetail`? | **No.** Other DTOs in `mt5_types.h` have codecs; `GroupDetail` does not. |
| Is the stub unit-tested? | **No.** HTTP tests never call `GetGroupDetails`. The time-window fake also returns `false` so a test double can compile. |
| Does C# already consume this? | **No C++ symbol.** `IMt5BrokerConnector.GetGroupsAsync` is served by `FakeMt5BrokerConnector` canned `Mt5GroupDto`s. There is no live HTTP adapter in `src/Mt5`. |

**One-liner:** remote `GetGroupDetails` is an intentional fail-closed stub, not a half-wired client. Names can come from `GET /mt5/groups`; structured group rows cannot come from `MT5HttpClient`.

---

## 2. The stub (quote)

Header — it is a required override, not an inherited default:

```70:73:D:\Prop\mt5-sdk\src\core\mt5_http_client.h
    uint32_t GroupTotal() override;
    bool GetAllGroups(std::vector<std::string>& groups) override;
    bool GetGroupDetails(std::vector<GroupDetail>& groups) override;
    bool GetGroupLogins(const std::wstring& group, std::vector<uint64_t>& logins) override;
```

Body — entire implementation:

```666:670:D:\Prop\mt5-sdk\src\core\mt5_http_client.cpp
bool MT5HttpClient::GetGroupDetails(std::vector<GroupDetail>& groups) {
    // Not implemented for HTTP client proxy mode — always returns false.
    // Group detail queries require direct SDK access via MT5Manager.
    return false;
}
```

Facts from those five lines:

1. Comment states the contract: **always returns false** in HTTP proxy mode.
2. Comment names the intended owner: **direct SDK via `MT5Manager`**.
3. There is **no** `(void)groups;` (unlike `IMT5Client::GetOrders` default). The out-param is named and unused.
4. There is **no** `groups.clear()`.
5. There is **no** `spdlog` line (local manager logs `"MT5 GetGroupDetails: returned {} groups"`).
6. There is **no** `IsConnected()` check. Health of `/mt5/health` is irrelevant; the method never looks at it.

---

## 3. Why this is an override-stub, not an interface default

`IMT5Client` group trio is **pure virtual**. Every concrete client must implement it:

```163:167:D:\Prop\mt5-sdk\src\core\imt5_client.h
    // ===== Group Operations =====
    virtual uint32_t GroupTotal() = 0;
    virtual bool GetAllGroups(std::vector<std::string>& groups) = 0;
    virtual bool GetGroupDetails(std::vector<GroupDetail>& groups) = 0;
    virtual bool GetGroupLogins(const std::wstring& group, std::vector<uint64_t>& logins) = 0;
```

Contrast `GetOrders`, which **does** have a fail-closed default on the interface, so `MT5HttpClient` does not override it:

```57:59:D:\Prop\mt5-sdk\src\core\imt5_client.h
    virtual bool GetOrders(uint64_t login, std::vector<OrderData>& out) {
        (void)login; (void)out; return false;
    }
```

**Correction to A04 (stale on this point):** A04 said `GetOrders`/`GetGroupDetails` are “**not** overridden” and “stay at interface defaults.” That is true for `GetOrders`. It is **false** for `GetGroupDetails`: there is no interface default; `MT5HttpClient` **must** override, and the override is the stub above. A16 / A84 already stated this correctly.

---

## 4. Sibling HTTP group methods (so the gap is isolated)

Same file, same class. These **do** hit the wire:

```654:677:D:\Prop\mt5-sdk\src\core\mt5_http_client.cpp
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

| Method | HTTP literal | Success shape | Fills out-param |
|---|---|---|---|
| `GroupTotal` | `GET /mt5/groups/count` | `count` (default `0`) | n/a (returns `uint32_t`) |
| `GetAllGroups` | `GET /mt5/groups` | `success` + `groups[]` of strings | **Yes** — assigns the vector |
| `GetGroupDetails` | **none** | **always `false`** | **No** |
| `GetGroupLogins` | `GET /mt5/groups/` + utf8(name) + `/logins` | `success` + `logins[]` | **Yes** |

`httpGet("/mt5/groups…")` appears **three** times in `mt5_http_client.cpp` (count, list, logins). A repo-wide search for `groups/details` under `D:\Prop\mt5-sdk` finds **no** path literal. Group names on the logins path are **not** URL-encoded (A16).

A16 §2 REST table matches this: count / list / logins are listed; details is listed under “operations with **no** HTTP path.”

---

## 5. What a real implementation looks like (local only)

`MT5Manager::GetGroupDetails` (`mt5_manager.cpp` 984–1013) is the only populator:

- lock `m_mutex`; require `m_manager && m_connected` else `false`
- `groups.clear()` + `reserve(GroupTotal())`
- `GroupCreate()` → `GroupNext(i, grp)` for `i ∈ [0, total)`
- on `MT_RET_OK`, fill `GroupDetail`:
  - `name` ← `Group()` UTF-8
  - `currency` ← `Currency()` UTF-8
  - `currency_digits` ← `CurrencyDigits()`
  - `company` ← `Company()` UTF-8
  - `margin_call` ← `MarginCall()`
  - `margin_stop_out` ← `MarginStopOut()`
  - `connections_allowed` ← `(PermissionsFlags() & 0x00000002) != 0` (`PERMISSION_ENABLE_CONNECTION`)
- `Release()`, log count, return `true`

No plan-map / `MT5_GROUP_*` filter. No `GroupRequest` / `GroupRequestArray` (A84). Partial `GroupNext` failures are skipped and the method still returns `true` (A14 caveat: size can be `< GroupTotal()`).

`GroupDetail` itself:

```60:68:D:\Prop\mt5-sdk\src\core\mt5_types.h
struct GroupDetail {
    std::string name;              // group name (e.g. "real\challenge_phase1_10k")
    std::string currency;          // deposit currency (e.g. "USD")
    uint32_t    currency_digits = 2;
    std::string company;           // company label on this group
    double      margin_call    = 0; // margin call level (%)
    double      margin_stop_out= 0; // stop-out level (%)
    bool        connections_allowed = false; // PERMISSION_ENABLE_CONNECTION
};
```

`mt5_types.h` has nlohmann `to_json`/`from_json` for `UserData`, `AccountData`, `PositionData`, `DealData`, `OrderData`, `SymbolData`, `TickData`, `ChartBarData`, calendar types. **`GroupDetail` has none.** Even if someone added `GET /mt5/groups/details` tomorrow, this client has no codec to parse it today.

---

## 6. Other walkers

### 6.1 `MT5Session` (pool) — not `IMT5Client`

```79:82:D:\Prop\mt5-sdk\src\core\mt5_pool.h
    // Group Operations
    uint32_t GroupTotal();
    bool GetAllGroups(std::vector<std::string>& groups);
    bool GetGroupLogins(const std::wstring& group, std::vector<uint64_t>& logins);
```

No `GetGroupDetails`. Pool-backed discovery cannot produce `GroupDetail` without going through `MT5Manager` directly.

### 6.2 Time-window test double

`D:\Prop\mt5-sdk\tests\mt5_time_window_test.cpp:37`:

```cpp
bool GetGroupDetails(std::vector<GroupDetail>&) override { return false; }
```

Compile-only fake. Not a test of the HTTP stub.

### 6.3 HTTP pool tests

`mt5_http_client_pool_timeout_test.cpp` uses `MT5HttpClientPoolTestAccess` and only drives `httpPost` / acquire / shutdown. **Zero** `GetGroupDetails` calls. Calendar tests construct `MT5HttpClient("http://127.0.0.1:9", …)` to prove news/calendar does not hit the network; they do **not** assert the group-details stub.

### 6.4 `mt5_group_probe`

`tests/mt5_group_probe.cpp` 86–90: if `MT5_MODE=remote`, print failure and **exit 3**. It never constructs `MT5HttpClient`. It only calls `MT5Manager::GetAllGroups` (names), **not** `GetGroupDetails`.

Probe comment at 87–88 is **stale**: *“The remote HTTP client does not expose a group-list endpoint.”* That is false for names — `GetAllGroups` **does** `GET /mt5/groups`. It is true for **details**. A18 is the accurate reading: do not “fix” the probe by silently switching to HTTP; remote group list is a different, untested path. A39’s “refuses remote because `GetGroupDetails` is unimplemented” over-states the probe’s actual reason (it refuses all remote group work, and it never asked for details).

---

## 7. Caller / product impact

### 7.1 C++ callers

No production C++ caller under `D:\Prop\mt5-sdk` except `MT5Manager` itself and the time-window fake. Services that bind `IMT5Client&` and need `GroupDetail` **must** be on `MT5Manager` (local) or treat `false` as `dependency_unavailable`.

A58 / A40 policy that still applies: if details are unsupported, **do not persist “broker has zero groups.”** Name-only remote (`GetAllGroups` true) may upsert names with empty currency/company until details exist. Do **not** fall back to `MT5_GROUP_*` mapping paths.

### 7.2 C# product (`D:\Prop\src`)

| Surface | State |
|---|---|
| `GetGroupDetails` symbol | **Absent** |
| `IMt5BrokerConnector.GetGroupsAsync` | Returns `IReadOnlyList<Mt5GroupDto>` |
| `Mt5GroupDto` fields | `Name, Currency, CurrencyDigits, Company, MarginCall, MarginStopOut, ConnectionsAllowed` — same shape as `GroupDetail` |
| Live implementor | **None.** `FakeMt5BrokerConnector` + `DemoBrokerFactory` hard-codes 3 ACHIEVER + 1 STARWAVEFX rows |
| Ingestion | `DealIngestionService.SyncBrokerAsync` upserts whatever `GetGroupsAsync` returns |
| HTTP `/mt5/*` in C# | **0** literals (D04) |

A future C# remote connector that called only `GET /mt5/groups` would get names. It cannot fill `Mt5GroupDto` currency / company / margins from `MT5HttpClient::GetGroupDetails`. Inventing those fields from plan maps is forbidden (A39 / A40 / B32).

### 7.3 Proposed but unbuilt path (A30)

```text
GET /mt5/groups/details                  # NEW — GetGroupDetails (A16: missing)
```

This route is **not** in the client, **not** in a collector in this tree, and **not** wired. Confirming the stub is **not** permission to add it in this task.

---

## 8. Comparison with the other HTTP stubs

| Method | Kind | Behaviour |
|---|---|---|
| `GetGroupDetails` | **Required override, hard `false`** | No HTTP, no out-param write, no reason string |
| `GetNewsCalendarItems` / `GetCalendarEvents` | Override, structured unsupported | `supported=false`, `status="unsupported"`, explicit reason: no remote bridge endpoint; **also** no HTTP |
| `GetOrders` / `GetChart` / `SubscribeTicks` | **Inherited interface default** | `false` / empty; HTTP client does not mention them |
| `SendTrade` pending/modify/cancel | Override, local refuse | `supported=false`, `ok=false`; does not invent extra REST paths |

`GetGroupDetails` is the poorest stub: bool only, no `reason`, no log. Callers cannot distinguish “unimplemented on this transport” from “remote returned failure” without knowing they are on `MT5HttpClient`.

---

## 9. What this note does **not** claim

- That a remote microservice *somewhere* already implements `/mt5/groups/details`. This repo’s client never calls it.
- That `GET /mt5/groups` is live-tested against Achiever / StarwaveFX. The probe refuses remote; HTTP tests do not hit that path.
- That `GroupTotal()==0` vs disconnected is distinguishable on HTTP (`count` default 0).
- That C# workers already compose `GetAllGroups` + `GetGroupDetails`. They do not.
- That the stub should be “fixed” in this pass. Assigned work is confirm + write this report.

---

## 10. Confirm checklist (done)

- [x] `MT5HttpClient::GetGroupDetails` exists as an override.
- [x] Body is a stub: comments + `return false`.
- [x] No REST/SSE/curl in that method.
- [x] No `/mt5/groups/details` literal in the HTTP client.
- [x] `GroupTotal` / `GetAllGroups` / `GetGroupLogins` remain real HTTP proxies.
- [x] Only `MT5Manager` fills `GroupDetail`.
- [x] `GroupDetail` has no JSON codec.
- [x] Pool session has no details method.
- [x] No product source was modified.

**CONFIRMED:** `MT5HttpClient::GetGroupDetails` is a fail-closed stub. Remote mode can list group **names** (untested path) and cannot return group **details**.
)
