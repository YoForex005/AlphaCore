# C50 — `TraderIntelligence.Api.http`: update needed?

| Field | Value |
|---|---|
| Agent | C50 (senior engineer, REST Client sample only) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\C50_http_file.md` |
| Ask | Read `apps/api/TraderIntelligence.Api.http`. **Update needed?** Write this report. |
| Product source modified | **No.** Report only. The `.http` file was **not** edited. |
| Left | `D:\Prop\apps\api\TraderIntelligence.Api.http` |
| Right | Live maps in `D:\Prop\apps\api\Program.cs` + web hooks in `D:\Prop\apps\web\src\api\hooks.ts` |
| Method | Full read of the `.http` file. SHA-256 + byte + line-ending census. Grep every `MapGet` / `MapPost` / `MapHub` in `apps/api`. Cross-read `launchSettings.json`, `hooks.ts`, `client.ts`, `signalr.ts`, DemoSeeder logins, A63 / A77 / B06 / B23 / B41 / C04 / C15. **API process was not launched.** The `.http` file was **not** sent. |
| Law | Architecture v2 §55 / §72.5 (no secrets in committed samples). A77 probes. A63 first-useful catalog is **future**, not today's host. |
| Relates | A06 (stale: file was weatherforecast on `:5160`), A30 (replace weather with `/health`), B06-01 (closed on disk), B23 leftover A (stale), B41 (port MATCH), C04 §5.2, C15 (weather token gone from `.http`) |
| Supersedes | B06-01 / B23 leftover-A / A19 `.http` note **as current file content**. Does **not** supersede B06 §5.3 as a *future* v1 sample (that catalog is still **MISSING** on the host). |

Classification vocabulary is architecture §73: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE` / `GONE`.

---

## 0. Headline answer

**Yes — update is needed.** Not because the weather template is still here (it is **gone**), and not because the port is wrong (it is **`:5000`**). Because the file is an **incomplete, poorly-formed** smoke list for the **live** 15-map host.

| Question | Answer |
|---|---|
| Does the file exist? | **Yes.** `D:\Prop\apps\api\TraderIntelligence.Api.http` (not under `D:\Prop\src\apps\api` — that path does not exist). |
| Is it still the stock `GET /weatherforecast/` on `:5160`? | **No.** That leftover is **GONE** (B06-01 / B23 leftover-A **closed**). |
| Does `@api` match the Kestrel `http` profile and the web fallback? | **Yes.** `http://localhost:5000`. **Do not** revert to `:5160`. |
| How many live host maps does it sample? | **7 of 15** (47%). Missing `/ready`, `/api/health`, `/api/settings`, `/api/trades`, `/api/reconciliation/status`, `/api/traders/{broker}/{login}`, `/api/risk/status`, `POST /api/ops/resync`. |
| How many React hooks does it sample? | **6 of 11** GETs in `hooks.ts`. |
| Is it a valid multi-request REST Client file? | **No.** Zero `###` separators, zero blank lines between method lines. VS / Rider / vscode-restclient treat this as **one** request (`GET /health`) plus six illegal header lines. |
| Should a coding wave paste B06 §5.3 (`/api/v1/**` + Bearer + login) today? | **No.** Those routes are **not mapped**. That paste would 404 everything except `/health` (and `/ready` if added). Sample the **live** unversioned surface first. |
| Product source edited by C50? | **No.** |

Honest one-liner: **the leftover is fixed; the smoke file is not done.**

Classification of this file:

| Aspect | Class |
|---|---|
| File presence | **EXISTS_NEEDS_REFACTOR** |
| Port `@api=:5000` | **EXISTS_AND_GOOD** |
| Weatherforecast / `:5160` tokens | **GONE** |
| Coverage vs live `Program.cs` maps | **PARTIAL** (7/15) |
| REST Client format (`###`, headers) | **EXISTS_NEEDS_REFACTOR** |
| A63 `/api/v1` samples | **MISSING** on host — do **not** add as executable requests yet |
| Secrets in the sample | **ABSENT** (good; keep it that way) |

---

## 1. Files hashed (this check)

| Path | Bytes | SHA-256 | CRLF | Role |
|---|---:|---|---|---|
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | 193 | `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651` | **LF only** | subject |
| `D:\Prop\apps\api\Program.cs` | 4658 | `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` | LF | live maps |
| `D:\Prop\apps\api\Properties\launchSettings.json` | 1133 | `E092DE590CC74329369741650044845BFFF555D87B75D7B0C3C80E00F6E00E78` | CRLF + BOM | listen table |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | 803 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` | CRLF | no LaunchProfile override |
| `D:\Prop\apps\web\src\api\hooks.ts` | 1935 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | LF | dashboard GETs |
| `D:\Prop\apps\web\src\api\client.ts` | 232 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | CRLF | `baseURL` `:5000` |

Hashes for `.http`, `Program.cs`, and `launchSettings.json` are **identical** to C04 / B41. The file has not moved since those reviews; C04 only noted the omissions. This agent answers the update question they left open.

Hex head of the `.http` file (first 81 bytes): `40-61-70-69-3D-68-74-74-70-3A-2F-2F-6C-6F-63-61-6C-68-6F-73-74-3A-35-30-30-30-0A-0A-47-45-54-20-…` = `@api=http://localhost:5000\n\nGET `. No BOM. No CR.

Drift vs earlier swarm snapshots of **this same path**:

| Wave | Bytes | SHA-256 | Content |
|---|---:|---|---|
| A55 / B06 / B23 | 157 | `353BB5D9718D6F86F218C1CE0885A55D8F49F68C249F04DA18E363DEA334543A` | `@…_HostAddress = http://localhost:5160` + `GET …/weatherforecast/` + `###` |
| C04 / B41 / **C50 (now)** | 193 | `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651` | `@api=http://localhost:5000` + 7 unversioned GETs, **no** `###` |

---

## 2. File as measured (complete)

`D:\Prop\apps\api\TraderIntelligence.Api.http` — 10 lines (trailing LF), entire contents:

```http
@api=http://localhost:5000

GET {{api}}/health
GET {{api}}/api/overview
GET {{api}}/api/brokers
GET {{api}}/api/groups
GET {{api}}/api/traders
GET {{api}}/api/fix/sessions
GET {{api}}/api/risk
```

Tokens that are **absent** (confirmed by read + hex):

| Token | Hits |
|---|---:|
| `weatherforecast` / `WeatherForecast` | **0** |
| `5160` | **0** |
| `###` | **0** |
| `Accept:` / `Content-Type:` / `Authorization:` | **0** |
| `POST` / `PATCH` / `PUT` / `DELETE` | **0** |
| `/api/v1` | **0** |
| `/ready` | **0** |
| password / secret / connection string | **0** |

---

## 3. Live host surface (what the sample must track)

`Program.cs` SHA `E914FA98…` maps **15** anonymous endpoints. `AddSignalR` / `MapHub` = **0**. `UseSwagger()` in Development only (no UI). No auth.

| # | Method | Path | In `.http`? | Web hook? | Notes |
|---|---|---|---|---|---|
| 1 | `GET` | `/health` | **yes** | no (web uses `/api/health`) | A77 liveness. Extra `utc` field vs A77 `{status:"ok"}`. |
| 2 | `GET` | `/api/health` | **no** | `useHealth` | Hardcoded demo inventory. System Health page. |
| 3 | `GET` | `/api/risk/status` | **no** | no | Alias of `/api/risk`. |
| 4 | `GET` | `/api/reconciliation/status` | **no** | `useReconciliation` | Hardcoded zeros. Reconciliation page. |
| 5 | `GET` | `/api/settings` | **no** | `useSettings` | Hardcoded flags. Settings page. |
| 6 | `GET` | `/ready` | **no** | no | A77 readiness. Counts `Brokers`. |
| 7 | `GET` | `/api/overview` | **yes** | `useOverview` | `OverviewDto`. |
| 8 | `GET` | `/api/brokers` | **yes** | `useBrokers` | `BrokerStatusDto`. |
| 9 | `GET` | `/api/groups` | **yes** | `useGroups` | `GroupRowDto`. |
| 10 | `GET` | `/api/traders` | **yes** (no query) | `useTraders` | Binds `broker`, `state`. Sample sends neither. |
| 11 | `GET` | `/api/traders/{broker}/{login}` | **no** | `useTraderDetail` | Demo logins `10001` / `99001`. |
| 12 | `GET` | `/api/fix/sessions` | **yes** | `useFixSessions` | `FixSessionDto`. |
| 13 | `GET` | `/api/risk` | **yes** | `useRiskStatus` | Same as `/api/risk/status`. |
| 14 | `GET` | `/api/trades` | **no** | `useTrades` | Raw EF, last 200. Optional `login`. `broker` unused. |
| 15 | `POST` | `/api/ops/resync` | **no** | **none** | Unauthenticated mutation. Must be last + commented if added. |

Coverage: **7 present / 8 missing** vs the host. **6 / 11** vs `hooks.ts`.

Not HTTP-mapped (do **not** add as a GET that is expected to 200):

| Client target | Host | If sampled today |
|---|---|---|
| `GET /hubs/dashboard` (`signalr.ts`) | **no hub** | 404 (C28) |
| `GET /swagger` (`launchUrl`) | `UseSwagger()` JSON only; **no** `UseSwaggerUI()` | UI 404; JSON may live at `/swagger/v1/swagger.json` |
| `GET /weatherforecast` | **unmapped** | 404. **Never re-add.** |
| Any `GET /api/v1/**` (A63 / B06 §5.3) | **0 maps** | 404 |

---

## 4. Why the format is broken (not just incomplete)

Microsoft `.http` / vscode-restclient / Rider HTTP Client: a request is `METHOD URL`, then headers until a blank line, then body. **Requests are separated by `###`.**

This file has **no** `###` and **no** blank line between the seven `GET` lines. The documented parse is:

```text
request-line:  GET http://localhost:5000/health
header:        GET {{api}}/api/overview     ← illegal header name (spaces / braces)
header:        GET {{api}}/api/brokers
… four more
```

So “Send Request” on the file is **not** a 7-request suite. At best `/health` fires with junk headers; at worst the client refuses to send. The stock file B06 replaced **did** have a trailing `###`. The retarget dropped the only correct separator.

Missing `Accept: application/json` is low. Missing `###` is the format defect.

LF-only line endings work in these clients. Not a reason to rewrite by itself.

---

## 5. What is **already good** (do not undo)

| Item | Evidence | Do not |
|---|---|---|
| Host `:5000` | Matches `profiles.http.applicationUrl`, README, Compose `5000:5000`, `client.ts` fallback (B41 **MATCH**) | Revert to `:5160` (A62 snippet / A06 text are stale) |
| No weatherforecast | 0 hits in this file (C15) | Add a “compat” alias |
| No secrets | No password, no Bearer live token, no connection string | Paste a real cTrader / MT5 password. B06 §5.3 `<SECRET>` login is for a **future** auth route that does not exist. |
| Variable name `@api` | Fine | Rename for style |
| Unversioned `/api/*` | Matches **today’s** `MapGet`s and `hooks.ts` | Replace the executable list with `/api/v1/*` before those maps exist |

B06-01 (“`.http` still weatherforecast on `:5160`”) is **done on disk**. Do not re-open it from B06 / B23 / A19 prose.

---

## 6. Update needed — finding list

| ID | Sev | Finding | Action when a coding wave is authorized |
|---|---|---|---|
| **C50-01** | **MED** | 8 of 15 live maps are unsampled. An operator cannot smoke the dashboard surface from this file. | Add the missing live GETs. See §8. |
| **C50-02** | **MED** | No `###` separators; consecutive `GET` lines. File is one malformed request. | Split every request with `###`. Restore what the stock template had. |
| **C50-03** | **MED** | React pages that 200 on the host (`/api/health`, `/api/settings`, `/api/trades`, `/api/reconciliation/status`, trader detail) have no sample. | Add one request per `hooks.ts` GET. |
| **C50-04** | **LOW** | `/ready` omitted. A77 + B06 said the replacement sample **starts** with `/health` then `/ready`. | Add `GET {{api}}/ready` immediately after `/health`. |
| **C50-05** | **LOW** | `GET /api/traders` does not show `?broker=&state=`. Host binds both. | Add a second named request with `broker=ACHIEVER&state=WATCH`. |
| **C50-06** | **LOW** | No `Accept: application/json`. | Add on every GET. |
| **C50-07** | **LOW** | Only mutation `POST /api/ops/resync` is unsampled. It is anonymous and **UNSAFE** (C04-07). | Add **last**, behind `###`, with a `# WARNING: unauthenticated write` comment. Empty body. Do not auto-fire. |
| **C50-08** | **INFO / PASS** | Weatherforecast + `:5160` leftovers are gone. Port matches web. No secrets. | Keep. |
| **C50-09** | **INFO** | B06 §5.3 is the **wrong** next paste. `/api/v1/auth/login` and Bearer routes 404 today. | Keep a `# future /api/v1` comment block **disabled**, or omit until maps exist. |
| **C50-10** | **INFO** | IIS Express `launchUrl` is still `weatherforecast` (C04-03 / C15). Out of this file. | Do not “fix” that by adding `/weatherforecast` here. |

**Verdict: update needed = YES** (C50-01 + C50-02 are enough). Severity is **sample-quality**, not a runtime defect in `Program.cs`.

---

## 7. What this file is **not** for

| Temptation | Why not |
|---|---|
| Become the A63 catalog | 46 v1 routes + hub are **MISSING**. A `.http` that 404s is worse than a short live list. |
| Become an integration test | No assertions, no CI runner. `tests/Integration` owns that. |
| Probe IIS Express `:18720` | Lab path is Kestrel `:5000` (B41). Optional second `@apiIis` is noise. |
| `GET /hubs/dashboard` | SignalR negotiate, not REST. Hub is unmapped (C28). |
| Commit `Authorization: Bearer eyJ…` | Secret. There is no login route to mint one. |

---

## 8. Authorized later replacement (do **not** apply in C50)

When a coding wave is allowed to touch `apps/api/TraderIntelligence.Api.http` **only**, replace the file with a live-host smoke list. Do **not** add weatherforecast. Do **not** switch `@api` off `:5000`. Do **not** add real passwords.

Seeded compound keys (DemoSeeder + `POST /api/ops/resync` rebuild list): `ACHIEVER/10001`, `ACHIEVER/10002`, `ACHIEVER/10003`, `STARWAVEFX/99001`.

```http
@api=http://localhost:5000

### liveness (A77) — replaces GET /weatherforecast/
GET {{api}}/health
Accept: application/json

### readiness (A77)
GET {{api}}/ready
Accept: application/json

### dashboard inventory (not a probe; web System Health)
GET {{api}}/api/health
Accept: application/json

###
GET {{api}}/api/overview
Accept: application/json

###
GET {{api}}/api/brokers
Accept: application/json

###
GET {{api}}/api/groups
Accept: application/json

###
GET {{api}}/api/traders
Accept: application/json

###
GET {{api}}/api/traders?broker=ACHIEVER&state=WATCH
Accept: application/json

###
GET {{api}}/api/traders/ACHIEVER/10001
Accept: application/json

###
GET {{api}}/api/trades
Accept: application/json

###
GET {{api}}/api/trades?login=10001
Accept: application/json

###
GET {{api}}/api/fix/sessions
Accept: application/json

###
GET {{api}}/api/risk
Accept: application/json

### alias of /api/risk
GET {{api}}/api/risk/status
Accept: application/json

###
GET {{api}}/api/reconciliation/status
Accept: application/json

###
GET {{api}}/api/settings
Accept: application/json

### WARNING: anonymous mutation (C04-07). Do not send against a shared store by accident.
POST {{api}}/api/ops/resync
Accept: application/json

###
# Future first-useful catalog (A63 / B06 §5.3) — NOT mapped today. Do not uncomment until Program.cs grows /api/v1.
# GET {{api}}/api/v1/overview
```

Optional later: `GET {{api}}/swagger/v1/swagger.json` as a Dev-only OpenAPI check. Not required for dashboard smoke.

When `/api/v1` actually ships, **replace** this list (or add a second file). Do not run two conflicting executable catalogs in one file.

---

## 9. Honest limits

- Did not `dotnet run`. Did not send any request. 404 on `/weatherforecast` and `/api/v1/*` is inferred from missing `Map*` calls, not observed.
- Did not open the file in Visual Studio REST Client to screenshot the “Send Request” gutter. Parse claim follows the documented `###` rule.
- Did not re-scan `bin/` / `obj/` for a `WeatherForecast` type (C15 already: Debug API DLL clean).
- Did not edit `launchSettings.json` (IIS `launchUrl` leftover stays C04-03 / C15).
- Did not update `apps/web` or invent `/api/v1` maps.

---

## 10. Bottom line

| Ask | Measured answer |
|---|---|
| **Update needed?** | **Yes.** Grow the sample to the 15 live maps and insert `###` separators. |
| Weather / `:5160` leftover? | **Already gone.** Do not treat this as B06-01 still open. |
| Port? | **Keep `:5000`.** |
| Apply B06 §5.3 now? | **No** — those paths 404. |
| Product source edited? | **No.** |

Product source was not modified.
