# P501 — Spotware Open API (TCP/WebSocket) vs this product’s FIX 4.4

| Field | Value |
|---|---|
| Slot | **P501** |
| Agent | P501_WS_VS_FIX (senior trading-systems / FIX; research only) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Artifact | `D:\Prop\reports\swarm\20260818\P501_WS_VS_FIX.md` |
| Assigned URLs | https://help.ctrader.com/open-api/proxies-endpoints/ · https://help.ctrader.com/open-api/connection/ |
| Complements (auth / purpose / FIX) | https://help.ctrader.com/open-api/account-authentication/ · https://help.ctrader.com/open-api/creating-new-app/ · https://help.ctrader.com/open-api/ · https://help.ctrader.com/open-api/protocol-buffers-json/ · https://help.ctrader.com/fix/limitations/ · https://help.ctrader.com/fix/getting-credentials/ |
| Product source modified | **No.** This report is the only write. |
| Secret values printed | **None.** `CTRADER_FIX_PASSWORD` named as a key only. Tag 554 not quoted. No Open API `client_secret` exists in this tree to leak. |
| Method | `web_fetch` of the assigned official pages plus auth/getting-started/limitations. Repo grep + read of `src/Fix.CTrader`, `apps/fix-worker`, architecture §25–34 / §36, `.env` **key names only**, prior swarm A31/A33/A36/A73/A011/C19. No live socket. No `35=D`. No Open API connect. |

Classification vocabulary: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE` / `SAFE_BY_ABSENCE`.

---

## 0. Verdict (binding)

**Do not add a cTrader Open API WebSocket (or TCP protobuf/JSON) stack to this product.**

Spotware Open API is a **different product** from cTrader FIX 4.4. Official demo endpoints are `demo.ctraderapi.com:5035` (Protobuf) and `demo.ctraderapi.com:5036` (JSON); **TCP and WebSocket both work on those same host:port pairs**. Auth is **OAuth 2.0**: registered app `client_id` + `client_secret`, then an access token. **This repo has a FIX password and FIX CompIDs. It does not have an Open API client id.**

For **this** product (MT5 XAUUSD copy → Pepperstone/cServer hedge), the lowest miss-rate execution path is **persistent FIX 4.4 TRADE + QUOTE** on the issued EQX FIX hosts (`*:5211` / `*:5212` TLS), with sequence numbers, resend, persist-before-send `ClOrdID`, and `OrderStatusRequest` after a gap. A new WS stack does not inherit any of that, does not use the credentials we already hold, and adds OAuth / token / proxy / rate-limit miss modes.

**`1 ms` end-to-end from India to US-EQX is physically false.** Fiber one-way floor India→NYC is tens of milliseconds; commercial RTT is ~180–250 ms. Switching protobuf/JSON/WebSocket does not repeal the speed of light.

Open API remains the official **companion** for balance / margin / history (FIX Limitations). That is “alongside FIX,” not “instead of FIX,” and it is **out of scope** until persistent QUOTE+TRADE exist.

| Claim | Result |
|---|---|
| Official Open API demo ports | **`5035` Protobuf, `5036` JSON** on `demo.ctraderapi.com` |
| TCP and WebSocket on those ports | **Both supported** (same host:port; WS is `wss://`) |
| Open API auth | **OAuth app + access token** (`client_id` required) |
| This repo has Open API `client_id` / secret / token | **No** — `MISSING` |
| This repo has FIX password + QUOTE/TRADE CompIDs | **Yes** (keys present; values not printed here) |
| Lowest miss-rate for *this* product | **Persistent FIX TRADE+QUOTE**, not a new WS stack |
| `1 ms` India → US-EQX E2E | **Physically false** |
| Product edited | **No** |

One-liner:

```text
OPEN API = demo.ctraderapi.com:5035 proto / :5036 JSON ; TCP+WS
AUTH = OAuth app + access token  —  REPO HAS FIX PASSWORD, NOT client_id
LOWEST MISS = PERSISTENT FIX TRADE+QUOTE
1ms INDIA→US-EQX = PHYSICALLY FALSE
NO PRODUCT EDIT
```

---

## 1. Official Open API — assigned pages (quoted, not paraphrased weaker)

Fetched 2026-08-18 from Spotware Help Centre.

### 1.1 Proxies and endpoints

Source: https://help.ctrader.com/open-api/proxies-endpoints/

Official table:

| Live | Demo |
|---|---|
| `live.ctraderapi.com:5035` (Protobufs) | `demo.ctraderapi.com:5035` (Protobufs) |
| `live.ctraderapi.com:5036` (JSON) | `demo.ctraderapi.com:5036` (JSON) |

Official constraints on that page:

- **Protobuf always requires port `5035` (and only this port).**
- **JSON always requires port `5036` (and only this port).**
- Demo and live are **fully separated**. A live socket cannot serve demo accounts and vice versa. Dual-env apps need **two** connections.
- **“The endpoints are the same for TCP and WebSocket connections. Note that ports `5035` and `5036` both support TCP and WebSocket connections.”**
- Clients are steered via **AWS Global Accelerator** to “the closest proxy.” You do **not** pin a US-EQX box the way FIX `demo-us-eqx-01.p.c-trader.com` / `live-us-eqx-01.p.c-trader.com` pins Equinix.

These are **not** the FIX hosts or ports in this repo.

### 1.2 Establish a connection

Source: https://help.ctrader.com/open-api/connection/

- Connect to an Open API proxy with **TCP or WebSocket**.
- **TCP must use SSL** (“otherwise you will not be able to connect or interact with the API”). Official C# sample: `TcpClient` + `SslStream.AuthenticateAsClientAsync(Host)` on port **5035**.
- **WebSocket uses the same host and port.** Official C# sample builds `wss://{Host}:{Port}` and parses `ProtoMessage` from **binary** frames. `IsReconnectionEnabled = false`, `ReconnectTimeout = null`, `ErrorReconnectTimeout = null`.
- **“The Python SDK does not support the WebSocket standard.”**
- Best practices (official):
  - **At most two connections:** one demo, one live. Each may multiplex many accounts of that type.
  - After connect, run the **app authorisation flow**. Any message before app auth is an error.
  - Keep-alive: send **`ProtoHeartbeatEvent` every 10 seconds**.
  - Use a message queue; avoid concurrent send/receive.

That last block is a **consumer-app** session model (one multiplexed proxy, 10 s heartbeat, reconnect is your problem). It is not FIX sequence recovery.

### 1.3 Auth — why a FIX password cannot log on here

Source: https://help.ctrader.com/open-api/account-authentication/

> “The cTrader Open API authentication process is based on the OAuth 2.0 standard.”

Required pieces that **do not exist in this repo**:

| Official item | What it is | In `D:\Prop`? |
|---|---|---|
| Registered application at https://openapi.ctrader.com | Spotware **evaluates and approves** apps (`creating-new-app`) | **No** |
| `client_id` | Query param on `id.ctrader.com` grant URL; field on `ProtoOAApplicationAuthReq` | **No** |
| `client_secret` | Token exchange + `ProtoOAApplicationAuthReq` | **No** |
| Redirect URI | First-party OAuth redirect; Playground URI must not be used in production | **No** |
| Authorisation `code` | **1 minute** TTL | **No** |
| `accessToken` | ~**2,628,000 s (~30 days)**; signs subsequent messages | **No** |
| `refreshToken` | No documented expiry; used to mint a new access token | **No** |
| `ProtoOAApplicationAuthReq` | App auth **before** any other proto message | **No code** |
| `ProtoOAAccountAuthReq` | Account auth with `ctidTraderAccountId` + access token | **No code** |

Token URL is REST on `https://openapi.ctrader.com/apps/token` (`grant_type=authorization_code` or `refresh_token`). Scopes: `accounts` (view) or `trading` (trade).

A cTrader **FIX password** (tag 554) and **account id** (tag 553) are the FIX Logon secret. They are **not** an OAuth `client_id`. They cannot authorise `demo.ctraderapi.com:5035`. Conversely, an Open API access token cannot replace FIX Logon on `*:5211` / `*:5212`.

### 1.4 What Open API is *for* (official use cases)

Source: https://help.ctrader.com/open-api/

Open API is “for anyone with a cTrader ID (cTID) to create an application.” Official examples: wearable P&L, Telegram trade-alert bot, custom terminal that funnels new broker accounts, mobile market-overview + generative AI. Quote:

> “cTrader Open API is perfect for professional traders who want to engage socially and interact with their followers.”

Rate limits (same page): **50 requests/s/connection** non-historical; **5 requests/s/connection** historical.

This product is **not** a social/cTID app. It is an internal hedge copier onto **one** Pepperstone execution account. Official Open API purpose does not match.

Protobuf vs JSON (https://help.ctrader.com/open-api/protocol-buffers-json/): JSON is the “easy / human-readable” path on **5036**; Protobuf is the compact path on **5035**. Official framing for TCP protobuf is 4-byte little-endian length + `ProtoMessage`. That is a **third codec** next to FIX `8=FIX.4.4` SOH streams. JSON 5036 is the worst of the three for miss-rate and parse cost.

---

## 2. Official FIX 4.4 — the protocol this product already chose

Primary law: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §25–34, plus official Help:

| Page | URL |
|---|---|
| Limitations | https://help.ctrader.com/fix/limitations/ |
| Get credentials | https://help.ctrader.com/fix/getting-credentials/ |
| Prior swarm (not re-fetched as primary) | `A31_ctrader_fix_overview.md`, `A33_ctrader_fix_send_recv.md`, `A36_ctrader_data_dictionary.md` |

### 2.1 Two sockets, two purposes (official)

https://help.ctrader.com/fix/getting-credentials/ :

> “There are 2 types of connection, price connection and trade connection, and each type has its own separate set of credentials. Trading operations requests cannot be sent through the price connection's credentials and vice versa.”

This is **exactly** the QUOTE / TRADE split already in architecture §27 and in `CTraderFixOptions`.

https://help.ctrader.com/fix/limitations/ :

- FIX operation set is **closed** (RoE). Not extensible.
- Primary purposes: (1) live market data (2) trading operations.
- **No** balance / leverage / margin.
- **No** historical market data — “designed for the **fastest possible connection**.”
- Spotware points at Open API as **another API to use alongside FIX** for account information.

Official implication for P501: Open API is the documented **complement for account fields**, not the documented **execution venue** for a system that already has FIX credentials.

### 2.2 What this repo actually holds (keys only)

Product config surface (no secret values):

| Key / default | Role | Protocol |
|---|---|---|
| `CTRADER_FIX_HOST` default `demo-us-eqx-01.p.c-trader.com` (arch live: `live-us-eqx-01.p.c-trader.com`) | **FIX** gateway, US Equinix | FIX 4.4 TLS |
| `CTRADER_FIX_ACCOUNT_ID` | FIX tag **553** username (integer account) | FIX |
| `CTRADER_FIX_PASSWORD` | FIX tag **554** | FIX |
| `CTRADER_FIX_QUOTE_SSL_PORT=5211` / plain `5201` | Price session | FIX QUOTE |
| `CTRADER_FIX_TRADE_SSL_PORT=5212` / plain `5202` | Trade session | FIX TRADE |
| `CTRADER_FIX_*_SENDER_COMP_ID` (`demo.pepperstone.*` / arch `live.pepperstone.*`) | Tag 49 | FIX |
| `CTRADER_FIX_*_TARGET_COMP_ID=cServer` | Tag 56 | FIX |
| `CTRADER_FIX_*_SENDER_SUB_ID` / `TARGET_SUB_ID` = `QUOTE` / `TRADE` | Tags 50 / 57 | FIX |

Grep of `D:\Prop\src` for `client_id`, `client_secret`, `CTRADER_OPEN`, `openapi.ctrader`, `ProtoOA`: **0 hits**.

`.env` / architecture §56 catalog **FIX** keys. They do **not** catalog Open API `client_id`.

`CTraderFixSession.BuildLogon` emits `8=FIX.4.4`, `35=A`, `108=30`, `553`/`554`. That packet is meaningless on `:5035` / `:5036`.

### 2.3 Measured FIX adapter state (honesty — do not greenwash)

Persistent dual-session FIX is the **target**. It is **not** fully implemented. A WS rewrite does not close these gaps; it abandons them.

| Slice | Class | Evidence |
|---|---|---|
| Credential shape (host, 5211/5212, CompIDs, 553/554) | `EXISTS_AND_GOOD` as config | `CTraderFixOptions.cs`, `.env` key names, arch §25 |
| One-shot TLS `35=A` | `EXISTS_NEEDS_REFACTOR` | `CTraderFixSession.TryLogonAsync` connects, writes Logon, reads one buffer, **disposes** the socket |
| Persistent QUOTE/TRADE initiator (heartbeat, seq, resend) | **`MISSING`** | No QuickFIX/n (`C19_quickfix_not_wired.md`; `Fix.CTrader.csproj` has no `QuickFIXn.*`) |
| Live `NewOrderSingle` (`35=D`) in workers | **`SAFE_BY_ABSENCE`** | Logon host log: “NewOrderSingle still unimplemented.” `apps/fix-worker/Worker.cs` stamps `Disconnected` every 15 s |
| Session-row persist | `EXISTS_NEEDS_REFACTOR` | `CTraderFixLogonHostedService.PersistAsync` is **update-only** (A011) |
| Worker vs logon race | `UNSAFE` as status authority | Worker overwrites both rows to `Disconnected` / “No live QUOTE/TRADE socket” |
| Open API client | **`MISSING`** (correct absence) | No proto, no OAuth, no `:5035` |

Architecture §1.8 / §27: prefer QuickFIX/n; two independent session objects; do not share sequence counters; do not write a raw `TcpClient` engine unless necessary. The current raw logon helper is a **probe**, not the destination design. Replacing it with Open API WS would violate that law twice (new protocol + still not an engine).

---

## 3. Side-by-side (this product only)

| Axis | Open API TCP/WS (`demo.ctraderapi.com`) | FIX 4.4 QUOTE+TRADE (`*-us-eqx-01.p.c-trader.com`) |
|---|---|---|
| Official host class | AWS Global Accelerator **proxy** | **Broker-issued FIX gateway** (US-EQX in this lab) |
| Ports | **5035** proto / **5036** JSON | **5211** QUOTE TLS / **5212** TRADE TLS (plain 5201/5202 exist; not the production default) |
| Transports | TCP+SSL **and** `wss://` on the **same** ports | Persistent TLS TCP. Not WebSocket |
| Codec | Protobuf `ProtoMessage` (4-byte LE length) or JSON | `8=FIX.4.4` SOH tags; cServer DD (`FIX44-CSERVER.xml`, A36) |
| Auth | OAuth app + access token + `ProtoOA*Auth*` | FIX Logon `35=A` + tags 553/554 + Comp/Sub IDs |
| Creds in this repo | **None** | **FIX password + CompIDs present** |
| App registration | Required; Spotware approval | None. Credentials from cTrader **FIX API** settings |
| Session split | One multiplexed connection per env (demo **or** live) | **Two** sockets; trade msgs illegal on price creds |
| Recovery | Official C# WS sample **disables reconnect**; no FIX seq | MsgSeqNum, `35=2` ResendRequest, `141` reset, `35=H` status (RoE / A33) |
| Heartbeat | **10 s** `ProtoHeartbeatEvent` or drop | Tag `108` (this probe uses **30**); quote HB may pause while MD streams (FAQ / A33) |
| Identity for copy | `clientMsgId` optional; proto order ids | Persist-before-send **`ClOrdID`**; ER `ExecID`; position `721` (arch §32–34, A42) |
| After disconnect mid-send | App must re-auth (token may still be valid) and **guess** order state | `EXECUTION_STATE_UNKNOWN` → status/recon; **never blind resend** (arch §34) |
| Rate limit | 50/s non-hist, 5/s hist | Streaming MD + session; RoE closed set |
| Token expiry | Access token **~30 days** (extra miss) | Password until rotated; no 30-day OAuth cliff |
| Official “fastest connection” | Not the claim. Consumer/proxy API | **Yes** (Limitations: designed for fastest connection; no history) |
| Official “alongside” | Account / history / extra | Execution + live MD |
| Fits architecture §25 | **No** | **Yes** (binding venue) |
| Extra miss modes if adopted as the send path | OAuth, approval, token refresh, anycast proxy hop, 10 s HB, no seq, JSON parse, 50/s cap, one socket for quotes+orders | Already budgeted: seq gap, single TRADE owner, stale quote/signal (A73) |

**Miss-rate (copy execution)** means: fraction of approved source intents that fail to become a known destination state (`filled` / `rejected` / `unknown-then-reconciled`) because the **wire** dropped, duplicated, or could not be queried.

FIX wins that metric on paper **if and only if** the session is **kept up**: sequence + resend + `35=H` + persist-before-send. A one-shot Logon that closes the socket has a **high** miss rate too. That is an argument to **persist FIX**, not to open `wss://demo.ctraderapi.com:5035`.

WebSocket does not add reliability here. Official sample turns **reconnect off**. WS still runs on the Open API **proxy**, still needs OAuth, still has no FIX resend. Browser-friendly transport is irrelevant to `apps/fix-worker`.

---

## 4. Why persistent FIX TRADE+QUOTE is the lowest miss-rate for *this* product

1. **We already possess the execution secret.** Operator env binds `CTRADER_FIX_PASSWORD` and dual CompIDs. Open API would wait on portal registration, Spotware review, redirect URI, and a 30-day token machine. That delay is 100% miss until it exists.

2. **Architecture already specified the recovery model.** §27 independent sequence/heartbeat/reconnect; §28 single TRADE owner; §32 never send from an MT5 callback; §33 persist identity **before** send; §34 disconnect-after-send → `UNKNOWN`, never re-submit. Open API has no `ClOrdID` / `35=H` / `35=2` in that contract.

3. **QUOTE can die without killing TRADE.** Dual sockets mean a quote gap rejects **new entries** (stale-quote guard, A72/A73) while CLOSE/REDUCE can still go out on TRADE. One Open API socket couples them: a proxy blip stalls both MD and orders.

4. **FIX Limitations say Open API is the side channel.** Use it later for balance/margin **alongside** a live FIX session. Using it **as** the session throws away the “fastest possible connection” API we were issued.

5. **A new stack is a second execution venue.** Two senders (FIX + Open API) on one account violate §28 (duplicate TRADE / double reports) and §71 (do not add a mesh). `A80_not_to_build.md` already bans extra complexity until measurements demand it. There is **no** measurement that WS misses less than FIX.

6. **Codec tax.** JSON `:5036` is officially the simple path. Copy-XAU does not need human-readable frames. Protobuf `:5035` still needs generated `ProtoOA*` types, a length prefix, and app+account auth. FIX 4.4 + `FIX44-CSERVER.xml` is the codec the RoE already defines for this account.

7. **Anycast ≠ colocation.** Open API’s AWS Global Accelerator sends the client to “the closest proxy.” This lab’s venue is **US-EQX** FIX. A proxy in ap-south-1 plus a US match engine is **more** hops, not fewer. FIX `demo-us-eqx-01` / `live-us-eqx-01` is the pin we have.

What “persistent” means (acceptance, not implemented):

```text
QUOTE TLS 5211  —  logged on, heartbeating, SecurityList + MD subscription held
TRADE TLS 5212  —  logged on, heartbeating, independent seq
both            —  reconnect + resend + (TRADE) OrderStatusRequest for in-flight ClOrdIDs
neither         —  disposed after one 35=A  (current probe)
neither         —  Open API wss://demo.ctraderapi.com:5035
```

Until those two FIX sockets stay up, copy miss-rate is dominated by **no session**, not by protobuf vs FIX.

---

## 5. `1 ms` India → US-EQX is physically false

Venue names in this tree (`demo-us-eqx-01.p.c-trader.com`, `live-us-eqx-01.p.c-trader.com`) are **US Equinix** (NY-area / Secaucus class). Operator/dev work in this swarm is **India** (prior reports stamp `+05:30`).

| Bound | Number | Why |
|---|---|---|
| Vacuum *c* Mumbai↔NYC great circle (~12,540 km) | **~42 ms one-way** | 12,540 km / 299,792 km/s |
| Fiber (~0.67*c*, refractive index ~1.47) on the same geodesic | **~61 ms one-way** | 12,540 / 204,000 km/s |
| Real fiber path (not a geodesic; typically EU or Pacific) | **~80–110 ms one-way** | 1.3–1.6× geodesic |
| Commercial RTT India → US-East | **~180–250 ms** typical | Two fiber trips + routers |
| `1 ms` E2E budget | **≤ ~204 km of fiber one-way** | 1e-3 s × 204,000 km/s |
| Same-building NIC-to-NIC | still **> 1 ms** once you add TLS, FIX parse, matching, ER | Application E2E ≠ ping |

Even **vacuum** India→NYC is ~42× too slow for a 1 ms one-way claim. RTT is ~80× too slow. A WebSocket vs raw TCP difference is **microseconds to low milliseconds on a LAN**, not 200 ms of ocean fiber.

Product E2E is not a ping. Architecture §36 hop list:

```text
MT5 → collector
collector → scoring
risk
FIX outbound
cServer acknowledgement
fill
total source-to-fill
```

If the source book is on an India/EU MT5 manager and the destination matcher is US-EQX, **source-to-fill cannot be 1 ms**. Colocating the FIX worker **in** US-EQX only removes the worker↔cServer ocean hop. It does not make the MT5 signal from another continent arrive in 1 ms.

A73 already records that §36 timestamps are mostly **MISSING**. There is **no** measured 1 ms number in product telemetry to defend. Any “Open API WS will give us 1 ms” pitch is false on physics **and** false on instrumentation.

Do not set `MaxSourceSignalAge` (code default 15 s, A73) or quote-age gates from a 1 ms fantasy. Measure hops after persistent FIX exists.

---

## 6. What would go wrong if we built the WS stack anyway

| Failure | Why it is worse than unfinished FIX |
|---|---|
| Cannot authenticate | No `client_id`. FIX password will be rejected on `:5035`. |
| App not approved | Portal + Spotware review. Zero copy during the wait. |
| Token expiry / refresh race | 30-day access token; refresh invalidates the old pair. A hung refresh = full miss. |
| Heartbeat 10 s vs FIX 30 s | Tighter keep-alive; a blocked thread drops the only socket (quotes **and** orders). |
| No sequence | After a WS drop, in-flight `NewOrder` (proto) has no `35=2`. Must invent an Open API recon that architecture never specified. |
| Dual-send risk | If FIX logon probe and Open API both live, one account, two execution channels. §28 forbids it. |
| Anycast path | Worker in India may pin to a nearby proxy, not US-EQX matching. Extra hop, extra jitter. |
| Rate limit | 50/s is fine for one account **until** a reconnect storm + symbol snapshot + order burst. FIX streams MD without that counter. |
| JSON 5036 | Larger frames, slower parse, same OAuth. No benefit to miss-rate. |
| Python WS unsupported | Irrelevant to C#, but official SDKs are not “WS-first.” C# sample disables reconnect. |
| Scope creep | New proto compiler, new auth service, new secrets, new dashboard. A80 / §71. |

Legal later use (not this increment): read-only Open API `accounts` scope **next to** persistent FIX, to fill the official FIX hole (balance/margin). Still needs an approved app. Still not a send path.

---

## 7. Decision record

| Option | Do it? | Reason |
|---|---|---|
| Persistent FIX QUOTE `5211` + TRADE `5212` (QuickFIX/n + RoE DD, seq/HB/resend, persist-before-send, `35=H` on gap) | **Yes — product path** | Issued creds, official fastest API, lowest miss-rate model, architecture §25–34 |
| One-shot `35=A` probe (current) | Keep only as diagnostics | Not a session. Worker must stop stamping `Disconnected` over a real logon |
| Open API TCP protobuf `:5035` as execution | **No** | Wrong auth, wrong host, no creds, worse recovery |
| Open API WebSocket `wss://demo.ctraderapi.com:5035` | **No** | Same as row above + reconnect-off sample + still OAuth |
| Open API JSON `:5036` | **No** | Slowest official Open API codec; still OAuth |
| Open API later for balance/margin **alongside** FIX | Maybe, after FIX persists | Official Limitations; not a substitute |
| Claim 1 ms India→US-EQX | **Forbidden** | Physically false |

**Do not edit product to add Open API.** Do not add `CTRADER_OPENAPI_*` keys. Do not open `:5035`/`:5036` from `Fix.CTrader`.

---

## 8. Evidence index (paths)

Official (fetched this slot):

- https://help.ctrader.com/open-api/proxies-endpoints/
- https://help.ctrader.com/open-api/connection/
- https://help.ctrader.com/open-api/account-authentication/
- https://help.ctrader.com/open-api/creating-new-app/
- https://help.ctrader.com/open-api/
- https://help.ctrader.com/open-api/protocol-buffers-json/
- https://help.ctrader.com/fix/limitations/
- https://help.ctrader.com/fix/getting-credentials/

Repo (read, not modified):

- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs`
- `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\docs\ctrader-fix.md`
- `D:\Prop\docs\architecture.md`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §25–34, §36
- `D:\Prop\reports\swarm\20260818\A31_ctrader_fix_overview.md`
- `D:\Prop\reports\swarm\20260818\A33_ctrader_fix_send_recv.md`
- `D:\Prop\reports\swarm\20260818\A36_ctrader_data_dictionary.md`
- `D:\Prop\reports\swarm\20260818\A73_copy_latency.md`
- `D:\Prop\reports\swarm\20260818\A011_fix_persist.md`
- `D:\Prop\reports\swarm\20260818\C19_quickfix_not_wired.md`
- `D:\Prop\reports\swarm\20260818\A80_not_to_build.md`

`.env` was inspected for **key names only**. Password bytes are not in this file.

---

## 9. Scope check

| Asked | Done |
|---|---|
| Read official proxies-endpoints + connection | **Yes** (`web_fetch`) |
| Compare to existing FIX 4.4 in `D:\Prop` | **Yes** |
| Record official `demo.ctraderapi.com:5035` proto / `:5036` JSON; TCP+WS | **Yes** §1.1 |
| OAuth app + token vs repo FIX password / no client id | **Yes** §1.3 / §2.2 |
| Lowest miss-rate = persistent FIX TRADE+QUOTE, not new WS | **Yes** §0 / §4 |
| 1 ms India→US-EQX physically false | **Yes** §5 |
| Do not edit product | **Honored** |

END
