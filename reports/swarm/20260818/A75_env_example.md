# A75 — Secret-safe `.env.example` (architecture §56)

| Field | Value |
|---|---|
| Agent | A75 (secret-safe example configuration) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A75_env_example.md` |
| Product source edited | **No** |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§7–9, 25–26, 41, 55–56 |
| Binding siblings | `A05`, `A06`, `A08`, `A19`, `A25` §3, `A30` I0, `A40`, `A46`, `A49`, `A54`, `A62` |
| Method | Read architecture §55–§56 + sibling swarm specs + `D:\Prop\.env.example` + `mt5-sdk/.env.example` + `CTraderFixOptions.cs` + `Mt5BrokerOptions.cs` + host `appsettings*.json`. Nothing answered from memory. |

**Overall verdict:** Architecture §56 is the **key catalog**. Architecture §55 is the **value law**: `.env.example` may contain **placeholders only**. Live venue identifiers that appear in §§7–8, 25, 56 (hosts, manager logins, FIX account, SenderCompID, egress, proxy) belong on a **non-git operator sheet**, not in the committed example. Both `SenderSubID` (tag 50) and `TargetSubID` (tag 57) are independently configurable per session. `REAL_COPY_EXECUTION_ENABLED=false` is mandatory in the example and is the config floor for any new `NewOrderSingle`.

This file is the implementation spec for a repo-root `.env.example`. **It does not write that file.** A later I0 increment may copy §4 verbatim.

---

## 0. What this note is (and is not)

This is the secret-safe example configuration required by architecture §55–§56.

It is **not**:

- a live operator sheet (do not paste issued IPs, logins, passwords, or CompIDs here),
- a license to enable real copy,
- an implementation of the env binder (A49),
- a rewrite of product `CTraderFixOptions` / `Mt5BrokerOptions` (those stay untouched in this pass).

Related controls that are **not** `.env.example` secrets:

| Control | Kind | In this example? |
|---|---|---|
| `STOP_NEW_EXECUTION` | Runtime kill switch (§40, A48) | No — not a deploy secret |
| `EMERGENCY_FLATTEN` | Runtime flatten (§40) | No |
| `READY_FOR_EXECUTION` | TRADE recon state (§42) | No — derived, never config-true |
| `SenderSubID` / `TargetSubID` | Per-session FIX headers (§26) | **Yes — configurable placeholders** |
| `REAL_COPY_EXECUTION_ENABLED` | Config floor (§41, §56) | **Yes — hard `false`** |

---

## 1. Binding law

### 1.1 §55 — Security

Never expose to React (or commit to Git):

```text
MT5 passwords
proxy credentials
cTrader account password
FIX password
database passwords
Redis passwords
```

Use environment variables, OS secret store, or Vault. Production secrets must not be committed.

**Quoted:** “Create only placeholders in `.env.example`.”

### 1.2 §56 — Secret-safe example configuration

§56 lists the **names** the product must accept, split as:

- Achiever MT5
- StarwaveFX MT5
- cTrader FIX execution
- feature flags ending in `REAL_COPY_EXECUTION_ENABLED=false`

§56’s last sentence is binding for headers:

> The engineer must populate SenderSubID/TargetSubID according to the current broker-issued FIX form and cTrader Rules of Engagement rather than guessing from labels.

### 1.3 §26 — both SubIDs configurable

Implementation must:

1. preserve exact broker-issued credentials (including case),
2. make **both** `SenderSubID` and `TargetSubID` configurable,
3. follow the current official cTrader RoE,
4. never silently change case (`cServer` → `CSERVER`) unless the issued form/spec requires it,
5. prove Logon on **both** sessions in diagnostics before enabling execution.

Do not collapse the form’s “SenderSubID = QUOTE / TRADE” onto tag 50 and leave tag 57 empty.

### 1.4 §41 — flag defaults (complete the §56 block)

§56 omitted `CTRADER_FIX_ENABLED`. §41 is the full safety surface:

```env
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
```

TRADE session **on** is not a send license. Only `REAL_COPY_EXECUTION_ENABLED=true` **plus** risk healthy **plus** `READY_FOR_EXECUTION` may emit `NewOrderSingle` (A25, A49).

---

## 2. Placeholder convention

| Token | Meaning | May a real value appear in `.env.example`? |
|---|---|---|
| `<SECRET>` | Password, API key, encryption key, Redis/DB password | **No** |
| `<BROKER_ISSUED_VALUE>` | Header / CompID / SubID copied from the **current** issued FIX form | **No** |
| `<MT5_MANAGER_HOST>` | Manager access point (host or IP, no scheme) | **No** |
| `<MANAGER_LOGIN>` | Manager account number (not a trading login) | **No** |
| `<SERVER_NAME>` | Friendly Manager server name | **No** |
| `<DISPLAY_NAME>` | Broker display label | Prefer placeholder |
| `<EGRESS_IP>` | Whitelisted outbound IP | **No** |
| `<PROXY_HOST>` / `<PROXY_PORT>` | Optional tunnel | **No** |
| `<CTRADER_FIX_HOST>` | cServer FIX gateway host | **No** |
| `<FIX_ACCOUNT_ID>` | FIX Username (553) = numeric trader login | **No** |
| `<PLAN_GROUP_PATH>` | Optional plan→group label (`demo\…`) | **No** live paths |
| `<POSTGRES_*>` / `<REDIS_HOST>` | Data-plane identity | Local generic names only; password still `<SECRET>` |

**Allowed as literal values** (public protocol / safety defaults, not venue identity):

| Key class | Why a literal is safe |
|---|---|
| Ports `443`, `5211`, `5212`, `5201`, `5202`, `5432`, `6379` | Published cTrader / MT5 / platform defaults |
| `CTRADER_FIX_QUOTE_SESSION_QUALIFIER=QUOTE` | Protocol session name, not an account secret |
| `CTRADER_FIX_TRADE_SESSION_QUALIFIER=TRADE` | Same |
| `CTRADER_FIX_USE_SSL=true` | Production transport law (§25) |
| `CTRADER_FIX_*_ENABLED=true` | §41 connect-without-trading |
| `REAL_COPY_EXECUTION_ENABLED=false` | **Required** safety floor |
| `MT5_MODE=local` / pool-size integers | Non-secret knobs |
| `ACHIEVER_PROXY_ENABLED=false` | Fail-closed example (operator may enable in `.env`) |

Do **not** treat “non-secret” as “safe to publish our live account.” Manager logins, FIX account ids, SenderCompIDs, live hosts, and egress IPs stay placeholders even though they are not passwords (A19-02).

---

## 3. Measured tree state (honest)

| Surface | Path | Classification |
|---|---|---|
| Architecture §56 | `MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 2031–2104 | **Operator sheet mixed into architecture.** Live hosts/logins/CompIDs + `<SECRET>` / `<BROKER_ISSUED_VALUE>` on passwords and SubIDs. |
| Repo-root example | `D:\Prop\.env.example` | **EXISTS — NOT placeholder-only.** Copies §7–§9 / §25 / §56 live IPs, logins, FIX account, SenderCompID, host, group paths. Passwords are `<SECRET>`. `REAL_COPY_EXECUTION_ENABLED=false` is correct. SubIDs are `<BROKER_ISSUED_VALUE>` (correct). Extra invented `FEATURE_*` / `RISK_*` blocks are **not** in §56. |
| SDK example | `D:\Prop\mt5-sdk\.env.example` | Placeholder-style for **one** MT5 broker. No `MT5_STARWAVEFX_*`, no `CTRADER_FIX_*`, no `REAL_COPY_*`. |
| Product hosts | `apps/{api,fix-worker,mt5-worker}/appsettings*.json` | Logging stubs only. **No** `ConnectionStrings`, **no** flag bind. |
| Options types | `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | Live host + `live.pepperstone.*` CompID compiled as defaults. `TargetCompId` default `CSERVER` (case-fold risk vs form `cServer`). `TargetSubId` hardcoded `QUOTE`/`TRADE`. `SenderSubId` empty (configurable). `RealCopyExecutionEnabled` default **false** (correct). |
| MT5 options | `src/Mt5/Configuration/Mt5BrokerOptions.cs` | Shape only. Password comment says secret placeholder. No env example binding. |
| Env binder | product `*.cs` | **MISSING.** Grep of `GetConnectionString` / `AddDbContext` in product hosts = 0. Flags are design law, not an implemented control (A08, A49). |
| React | `apps/web/src/api/client.ts` | `VITE_API_URL` fallback `http://localhost:5000`. No FIX/MT5 secrets (correct). |
| Git ignore of `.env` | `D:\Prop` | No root `.gitignore` observed in earlier A19. `mt5-sdk/.gitignore` covers SDK `.env` only. |

**Safety today:** live copy is off by **absence of a send path**, not by a bound `REAL_COPY_EXECUTION_ENABLED` gate. The example must still set the flag **explicitly false** so a future default-change cannot enable send (A49).

---

## 4. Canonical `.env.example` content

Copy the following block into a future repo-root `.env.example`. Do not substitute live operator values. Do not add passwords “to make local work” in the committed file.

```env
# =============================================================================
# Trader Intelligence — example environment (architecture §55–§56)
#
# Copy to `.env` and fill from the CURRENT broker-issued MT5 / FIX forms.
# Never commit `.env`. Never put secrets in appsettings.json or React `VITE_*`.
#
# Placeholders only. Operator sheet (non-git) holds live hosts / logins / CompIDs.
# SenderSubID (50) and TargetSubID (57) are independently configurable.
# REAL_COPY_EXECUTION_ENABLED must stay false until §68–§70 gates pass.
# =============================================================================

# -----------------------------------------------------------------------------
# Achiever MT5  (architecture §7, §56)
# Manager API identity — not a trading account.
# -----------------------------------------------------------------------------
MT5_SERVER=<MT5_MANAGER_HOST>
MT5_PORT=443
MT5_LOGIN=<MANAGER_LOGIN>
MT5_PASSWORD=<SECRET>
MT5_DEFAULT_GROUP=<PLAN_GROUP_PATH>
MT5_MODE=local
MT5_POOL_SIZE=8
MT5_SERVER_NAME=<SERVER_NAME>

ACHIEVER_EGRESS_IP=<EGRESS_IP>

# Optional proxy (off in the example; enable only in private `.env`)
ACHIEVER_PROXY_ENABLED=false
ACHIEVER_PROXY_HOST=<PROXY_HOST>
ACHIEVER_PROXY_PORT=<PROXY_PORT>
ACHIEVER_PROXY_USERNAME=<SECRET>
ACHIEVER_PROXY_PASSWORD=<SECRET>

# -----------------------------------------------------------------------------
# StarwaveFX MT5  (architecture §8, §56)
# Second source broker. Same connector code, separate credentials.
# -----------------------------------------------------------------------------
MT5_STARWAVEFX_DISPLAY_NAME=<DISPLAY_NAME>
MT5_STARWAVEFX_PROVISIONING_ENABLED=true
MT5_STARWAVEFX_MODE=local
MT5_STARWAVEFX_SERVER=<MT5_MANAGER_HOST>
MT5_STARWAVEFX_PORT=443
MT5_STARWAVEFX_LOGIN=<MANAGER_LOGIN>
MT5_STARWAVEFX_PASSWORD=<SECRET>
MT5_STARWAVEFX_SERVER_NAME=<SERVER_NAME>
MT5_STARWAVEFX_POOL_SIZE=4
MT5_STARWAVEFX_PROXY_ENABLED=false
MT5_STARWAVEFX_PROXY_HOST=<PROXY_HOST>
MT5_STARWAVEFX_PROXY_PORT=<PROXY_PORT>
MT5_STARWAVEFX_PROXY_USERNAME=<SECRET>
MT5_STARWAVEFX_PROXY_PASSWORD=<SECRET>

# -----------------------------------------------------------------------------
# Optional plan labels — NEVER the group fetch filter (architecture §9, A40)
# Discover all groups via Manager API, then overlay these labels.
# -----------------------------------------------------------------------------
MT5_GROUP_2STEP_DEMO=<PLAN_GROUP_PATH>
MT5_GROUP_1STEP_DEMO=<PLAN_GROUP_PATH>
MT5_GROUP_2STEP_REAL=<PLAN_GROUP_PATH>
MT5_GROUP_1STEP_REAL=<PLAN_GROUP_PATH>
MT5_GROUP_INSTANT_REAL=<PLAN_GROUP_PATH>
MT5_GROUP_CORE_DEMO=<PLAN_GROUP_PATH>
MT5_GROUP_CORE_REAL=<PLAN_GROUP_PATH>
MT5_GROUP_PASSFIRST_DEMO=<PLAN_GROUP_PATH>
MT5_GROUP_PASSFIRST_REAL=<PLAN_GROUP_PATH>

# -----------------------------------------------------------------------------
# cTrader FIX 4.4 execution venue (architecture §25–§26, §56)
# Not an LP. QUOTE and TRADE are two sockets, two sequence spaces.
# Populate CompIDs / SubIDs from the issued form + current RoE. Do not guess.
# Do not silently rewrite TargetCompID case (cServer vs CSERVER).
# -----------------------------------------------------------------------------
CTRADER_FIX_HOST=<CTRADER_FIX_HOST>
CTRADER_FIX_ACCOUNT_ID=<FIX_ACCOUNT_ID>
CTRADER_FIX_PASSWORD=<SECRET>
CTRADER_FIX_USE_SSL=true

# QUOTE session — TargetSubID is the session qualifier (tag 57).
# SenderSubID (tag 50) MUST be QUOTE when TargetSubID=QUOTE (RoE).
# Both remain independently configurable; do not hardcode in C#.
CTRADER_FIX_QUOTE_SSL_PORT=5211
CTRADER_FIX_QUOTE_PLAIN_PORT=5201
CTRADER_FIX_QUOTE_SENDER_COMP_ID=<BROKER_ISSUED_VALUE>
CTRADER_FIX_QUOTE_TARGET_COMP_ID=<BROKER_ISSUED_VALUE>
CTRADER_FIX_QUOTE_SESSION_QUALIFIER=QUOTE
CTRADER_FIX_QUOTE_SENDER_SUB_ID=<BROKER_ISSUED_VALUE>
CTRADER_FIX_QUOTE_TARGET_SUB_ID=<BROKER_ISSUED_VALUE>

# TRADE session — TargetSubID=TRADE is the qualifier.
# SenderSubID on TRADE is the issued originator string, NOT a guessed "TRADE".
CTRADER_FIX_TRADE_SSL_PORT=5212
CTRADER_FIX_TRADE_PLAIN_PORT=5202
CTRADER_FIX_TRADE_SENDER_COMP_ID=<BROKER_ISSUED_VALUE>
CTRADER_FIX_TRADE_TARGET_COMP_ID=<BROKER_ISSUED_VALUE>
CTRADER_FIX_TRADE_SESSION_QUALIFIER=TRADE
CTRADER_FIX_TRADE_SENDER_SUB_ID=<BROKER_ISSUED_VALUE>
CTRADER_FIX_TRADE_TARGET_SUB_ID=<BROKER_ISSUED_VALUE>

# -----------------------------------------------------------------------------
# Feature flags (architecture §41 + §56)
# Connect / quote / reconcile without placing new real orders.
# -----------------------------------------------------------------------------
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false

# -----------------------------------------------------------------------------
# Platform data plane (architecture §5, §55; A03, A54)
# Passwords stay placeholders. Do not put these in committed appsettings.json.
# -----------------------------------------------------------------------------
DATABASE_URL=Host=<POSTGRES_HOST>;Port=5432;Database=<POSTGRES_DATABASE>;Username=<POSTGRES_USERNAME>;Password=<SECRET>
REDIS_URL=<REDIS_HOST>:6379
REDIS_PASSWORD=<SECRET>

# ASP.NET nested aliases (optional). Same values. Never commit real passwords.
# ConnectionStrings__Postgres=Host=<POSTGRES_HOST>;Port=5432;Database=<POSTGRES_DATABASE>;Username=<POSTGRES_USERNAME>;Password=<SECRET>
# ConnectionStrings__TraderDb=Host=<POSTGRES_HOST>;Port=5432;Database=<POSTGRES_DATABASE>;Username=<POSTGRES_USERNAME>;Password=<SECRET>
# Redis__Configuration=<REDIS_HOST>:6379

# -----------------------------------------------------------------------------
# Web (A62) — no secrets, no connection strings
# -----------------------------------------------------------------------------
VITE_API_URL=
```

---

## 5. Key catalog

### 5.1 Achiever MT5

| Env name | Role | Example value | Secret |
|---|---|---|---|
| `MT5_SERVER` | Manager host / IP, no scheme | `<MT5_MANAGER_HOST>` | No (still placeholder) |
| `MT5_PORT` | Manager port | `443` | No |
| `MT5_LOGIN` | Manager login | `<MANAGER_LOGIN>` | No (still placeholder) |
| `MT5_PASSWORD` | Manager password | `<SECRET>` | **Yes** |
| `MT5_DEFAULT_GROUP` | Fallback group path only | `<PLAN_GROUP_PATH>` | No |
| `MT5_MODE` | `local` or `remote` | `local` | No |
| `MT5_POOL_SIZE` | Manager slot count, not CPU | `8` | No |
| `MT5_SERVER_NAME` | Label / metrics | `<SERVER_NAME>` | No |
| `ACHIEVER_EGRESS_IP` | Whitelist documentation | `<EGRESS_IP>` | No |
| `ACHIEVER_PROXY_ENABLED` | Tunnel master | `false` | No |
| `ACHIEVER_PROXY_HOST` | Tunnel host | `<PROXY_HOST>` | No |
| `ACHIEVER_PROXY_PORT` | Tunnel port | `<PROXY_PORT>` | No |
| `ACHIEVER_PROXY_USERNAME` | Tunnel user | `<SECRET>` | **Yes** |
| `ACHIEVER_PROXY_PASSWORD` | Tunnel password | `<SECRET>` | **Yes** |

### 5.2 StarwaveFX MT5

Same shape as Achiever under the `MT5_STARWAVEFX_*` prefix. §56 lists no Starwave proxy; §8 still requires the connector to accept one later. The four `MT5_STARWAVEFX_PROXY_*` keys are **adjacent** (allowed in the example, optional in `.env`).

`MT5_STARWAVEFX_PROVISIONING_ENABLED=true` is a product flag, not a send license.

### 5.3 Plan-group labels (§9)

Nine `MT5_GROUP_*` keys. Overlay only. Fetching **only** these groups is forbidden (A40).

### 5.4 cTrader FIX — venue identity

| Env name | FIX role | Example value |
|---|---|---|
| `CTRADER_FIX_HOST` | Socket host | `<CTRADER_FIX_HOST>` |
| `CTRADER_FIX_ACCOUNT_ID` | Logon 553 Username | `<FIX_ACCOUNT_ID>` |
| `CTRADER_FIX_PASSWORD` | Logon 554 Password | `<SECRET>` |
| `CTRADER_FIX_USE_SSL` | Transport | `true` |
| `CTRADER_FIX_QUOTE_SSL_PORT` | QUOTE TLS | `5211` |
| `CTRADER_FIX_QUOTE_PLAIN_PORT` | QUOTE plain (diagnostics) | `5201` |
| `CTRADER_FIX_TRADE_SSL_PORT` | TRADE TLS | `5212` |
| `CTRADER_FIX_TRADE_PLAIN_PORT` | TRADE plain (diagnostics) | `5202` |

Plain ports must not be the production default (§25). `CTRADER_FIX_USE_SSL=true` is the production transport.

### 5.5 cTrader FIX — independently configurable headers (§26, §56)

| Env name | Tag | Session | Example value | Guess from the word QUOTE/TRADE? |
|---|---|---|---|---|
| `CTRADER_FIX_QUOTE_SENDER_COMP_ID` | 49 | QUOTE | `<BROKER_ISSUED_VALUE>` | **No** |
| `CTRADER_FIX_QUOTE_TARGET_COMP_ID` | 56 | QUOTE | `<BROKER_ISSUED_VALUE>` | **No** (preserve case) |
| `CTRADER_FIX_QUOTE_SESSION_QUALIFIER` | QuickFIX qualifier | QUOTE | `QUOTE` | Protocol name |
| `CTRADER_FIX_QUOTE_SENDER_SUB_ID` | **50** | QUOTE | `<BROKER_ISSUED_VALUE>` | Fill from form; RoE requires `QUOTE` if 57=`QUOTE` |
| `CTRADER_FIX_QUOTE_TARGET_SUB_ID` | **57** | QUOTE | `<BROKER_ISSUED_VALUE>` | Fill from form; usually `QUOTE` |
| `CTRADER_FIX_TRADE_SENDER_COMP_ID` | 49 | TRADE | `<BROKER_ISSUED_VALUE>` | **No** |
| `CTRADER_FIX_TRADE_TARGET_COMP_ID` | 56 | TRADE | `<BROKER_ISSUED_VALUE>` | **No** (preserve case) |
| `CTRADER_FIX_TRADE_SESSION_QUALIFIER` | QuickFIX qualifier | TRADE | `TRADE` | Protocol name |
| `CTRADER_FIX_TRADE_SENDER_SUB_ID` | **50** | TRADE | `<BROKER_ISSUED_VALUE>` | **Not** a guessed `TRADE` |
| `CTRADER_FIX_TRADE_TARGET_SUB_ID` | **57** | TRADE | `<BROKER_ISSUED_VALUE>` | Fill from form; usually `TRADE` |

`FixSessionState` already has nullable `SenderSubId` / `TargetSubId` (`src/Domain/Entities/FixSessionState.cs`). Persistence of the **configured** strings is required; do not persist passwords.

### 5.6 Feature flags

| Env name | Example | Missing → |
|---|---|---|
| `CTRADER_FIX_ENABLED` | `true` | session objects may not start (A49: treat missing as architecture default `true`, but **set it**) |
| `CTRADER_FIX_QUOTE_ENABLED` | `true` | same |
| `CTRADER_FIX_TRADE_SESSION_ENABLED` | `true` | same |
| `REAL_COPY_EXECUTION_ENABLED` | **`false`** | **`false`** + warn. Unparseable / `"1"` / `"yes"` → **`false`** (A49). |

Parse `REAL_COPY_EXECUTION_ENABLED` only as case-insensitive `true` / `false`. Production **must set the key explicitly**.

---

## 6. How to fill SenderSubID / TargetSubID

Do not infer tag placement from the human form that prints `SenderSubID = QUOTE / TRADE`.

Official RoE (A25, A32):

| Tag | Name | Required | Rule |
|---|---|---|---|
| 57 | `TargetSubID` | Yes | Session qualifier: `QUOTE` or `TRADE` |
| 50 | `SenderSubID` | No | Originator id. **Must be `QUOTE` if `TargetSubID=QUOTE`.** On TRADE, any issued string — **not** automatically `TRADE`. |

Fill procedure (A25 §3.4):

1. Open the **current** broker-issued FIX form and the **current** cTrader RoE.
2. If the form prints one “session qualifier” field, write that value into `*_SESSION_QUALIFIER` **and** `*_TARGET_SUB_ID`.
3. Set `*_SENDER_SUB_ID`:
   - QUOTE: `QUOTE` when 57 is `QUOTE` (RoE).
   - TRADE: issued originator if present; else a stable configured string. Official examples use `any_string`. Do not invent a second semantic.
4. Copy `SenderCompID` / `TargetCompID` **verbatim**, including case.
5. Prove diagnostic Logon on **both** sessions (A25 §3.6) **before** any application message and long before `REAL_COPY_EXECUTION_ENABLED=true`.

If the form only prints one SubID, **still configure both keys**. Leaving `TARGET_SUB_ID` empty because the form said “SenderSubID” is a likely Logon failure.

---

## 7. `REAL_COPY_EXECUTION_ENABLED=false`

This is the entire point of the §56 / §41 default matrix.

```env
REAL_COPY_EXECUTION_ENABLED=false
```

| May do while false | Must not do |
|---|---|
| QUOTE Logon, SecurityList, market data | `NewOrderSingle` (35=D) for copy OPEN/INCREASE |
| TRADE Logon, heartbeat, resend | Catch-up 35=D after reconnect (§63) |
| OrderMassStatus / RequestForPositions | `OrderCancelReplace` that increases exposure |
| Persist + reconcile | Drain a backlog accumulated while the flag was off |
| Dashboard snapshot of the flag | React / SuperAdmin PATCH raising the flag above the **config floor** |

`EMERGENCY_FLATTEN` may send **reducing** orders while this flag is false **only** under A25 §6.5 / A48 (TRADE logged on, lease owned, persist-before-send, authorized). Flatten is not copy.

A30 I7 suggests `CTRADER_FIX_TRADE_SESSION_ENABLED=false` for an early increment that must not open TRADE at all. That is an increment constraint, **not** a rewrite of §41/§56. The example follows architecture: TRADE session **on**, real copy **off**.

Config is the floor (A25 §6.4, A49 §3.4):

```text
effective_real_copy =
      config.REAL_COPY_EXECUTION_ENABLED     -- this file; default false
  AND settings_store.real_copy               -- SuperAdmin PATCH; default false
  AND NOT STOP_NEW_EXECUTION
```

Runtime must **not** turn the gate on if the env value is `false`.

---

## 8. Adjacent recommended keys (not in the §56 block)

Optional in `.env.example` as comments. Do not treat them as §56 deliverables. Do **not** invent `FEATURE_COPY_TRADING_ENABLED` / `FEATURE_ML_SCORING_ENABLED` (those appear in the current root `.env.example` and are **not** architecture names).

```env
# --- A25 / A49 extra FIX gates (recommended) ---
# CTRADER_FIX_ALLOW_PLAINTEXT=false
# CTRADER_FIX_DIAGNOSTIC_LOGON_ONLY=false
# CTRADER_FIX_HEARTBT_INT=30
# CTRADER_FIX_RESET_SEQ_NUM=true
# MAX_QUOTE_AGE_MS=<MEASURED_NOT_GUESSED>

# --- A46 TRADE ownership (Redis lease + Postgres fence) ---
# TRADE_LEASE_TTL_MS=10000
# TRADE_LEASE_RENEW_MS=3000
# TRADE_LEASE_MIN_REMAINING_MS=2000
# TRADE_LEASE_ACQUIRE_BACKOFF_MS=1000
# TRADE_OWNERSHIP_ALLOW_DB_ONLY=false

# --- A35 QuickFIX store paths (never share QUOTE/TRADE files) ---
# CTRADER_FIX_QUOTE_FILE_STORE_PATH=store/quote
# CTRADER_FIX_QUOTE_FILE_LOG_PATH=log/quote
# CTRADER_FIX_TRADE_FILE_STORE_PATH=store/trade
# CTRADER_FIX_TRADE_FILE_LOG_PATH=log/trade

# --- SDK remote mode / at-rest crypto (mt5-sdk/.env.example) ---
# MT5_REMOTE_URL=https://<MT5_BRIDGE_HOST>
# MT5_API_KEY=<SECRET>
# MT5_PASSWORD_ENCRYPTION_KEY=<SECRET>

# --- A54 auth signing (Linux API only; never VITE_*) ---
# AUTH_JWT_SIGNING_KEY=<SECRET>
```

`MAX_QUOTE_AGE_MS` must be measured (A49). Do not ship a guessed number as if it were law.

HTTP `MT5_REMOTE_URL` is unsafe (A19-06). If the remote-mode line is included, use `https://`.

---

## 9. Binding contract (hosts will not map these for free)

Architecture names are **flat** (`CTRADER_FIX_QUOTE_SENDER_SUB_ID`). Default `Microsoft.Extensions.Configuration` nested binding expects `CTraderFix:Quote:SenderSubId` or `CTRADERFIX__QUOTE__SENDERSUBID`. **The default binder will miss `CTRADER_FIX_*`.**

A later fix-worker increment must use an **explicit map** (A49 §3.5):

| Architecture env | Existing options property |
|---|---|
| `CTRADER_FIX_HOST` | `CTraderFixOptions.Host` |
| `CTRADER_FIX_ACCOUNT_ID` | `AccountId` |
| `CTRADER_FIX_PASSWORD` | `Password` (UserSecrets / env only) |
| `CTRADER_FIX_USE_SSL` | `UseSsl` |
| `CTRADER_FIX_QUOTE_ENABLED` | `QuoteEnabled` |
| `CTRADER_FIX_TRADE_SESSION_ENABLED` | `TradeSessionEnabled` |
| `REAL_COPY_EXECUTION_ENABLED` | `RealCopyExecutionEnabled` |
| `CTRADER_FIX_QUOTE_SENDER_SUB_ID` | `Quote.SenderSubId` |
| `CTRADER_FIX_QUOTE_TARGET_SUB_ID` | `Quote.TargetSubId` |
| `CTRADER_FIX_TRADE_SENDER_SUB_ID` | `Trade.SenderSubId` |
| `CTRADER_FIX_TRADE_TARGET_SUB_ID` | `Trade.TargetSubId` |
| `CTRADER_FIX_ENABLED` | **MISSING** on `CTraderFixOptions` today |

Do not log `CTRADER_FIX_PASSWORD`, MT5 passwords, proxy passwords, `DATABASE_URL`, or Redis passwords. Do not log FIX tags 553/554 (A50). Log flags as structured booleans: `ctrader_fix_enabled`, `quote_enabled`, `trade_session_enabled`, `real_copy_execution_enabled`.

React may read `VITE_API_URL` and the **non-secret** flag snapshot from the API. It must never receive SubIDs if those are treated as broker-issued secrets (A06, A51).

---

## 10. What must never appear in `.env.example`

| Forbidden | Why |
|---|---|
| Any real password, API key, JWT signing key, encryption key | §55 |
| Live manager IPs / logins copied from architecture §7–§8 / §56 | A19-02; placeholder law |
| Live FIX host, account id, `live.*.<account>` SenderCompID | Same |
| Live egress / proxy host:port | Same |
| Live `demo\…` / `contest\…` group paths | Environment-specific; §9 catalog stays in architecture, not the example |
| `REAL_COPY_EXECUTION_ENABLED=true` | §41 / §56 / this task |
| Hardcoded `CTRADER_FIX_*_SENDER_SUB_ID=QUOTE` as the **only** TRADE/QUOTE mapping with empty TargetSubID | §26 |
| `VITE_CTRADER_FIX_PASSWORD` or any `VITE_*` secret | A62 |
| Connection strings with real passwords in `appsettings.json` | §55; A03 |

Architecture v2 may continue to document live identifiers as the **lab operator sheet**. That does not license copying them into the committed example.

---

## 11. Gaps to close later (do not implement in this pass)

| ID | Sev | Gap | Evidence |
|---|---|---|---|
| A75-01 | P2 | Root `D:\Prop\.env.example` embeds live §56 identifiers | File on disk vs §55 “placeholders only” |
| A75-02 | P2 | Product hosts do not bind any of these keys | `appsettings.json` logging-only; no `GetConnectionString` |
| A75-03 | P2 | `CTraderFixOptions` ships live host + CompID + `CSERVER` + hardcoded `TargetSubId` | `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` |
| A75-04 | P2 | No explicit env binder for flat `CTRADER_FIX_*` / `REAL_COPY_EXECUTION_ENABLED` | A49; options exist, binder does not |
| A75-05 | P3 | `CTRADER_FIX_ENABLED` missing from options type and from architecture §56 block | Present only in §41 |
| A75-06 | P3 | No root `.gitignore` for `.env` | A19-03 |
| A75-07 | Info | Current root example invents `FEATURE_*` / `RISK_*` names | Not in §56; drop on next rewrite |

Suggested later I0 (A30), **not done here**:

1. Replace `D:\Prop\.env.example` with §4 of this file.
2. Add root `.gitignore` for `.env`, `.env.*` except `.env.example`, `secrets.json`, `*.pfx`, `appsettings.*.local.json`.
3. Keep a private operator sheet out of Git for live IPs / logins / CompIDs.

---

## 12. Acceptance checks for the example file

When I0 writes `.env.example` from §4:

- [ ] Every §56 key name is present.
- [ ] `CTRADER_FIX_ENABLED` is present (§41).
- [ ] `CTRADER_FIX_QUOTE_SENDER_SUB_ID`, `CTRADER_FIX_QUOTE_TARGET_SUB_ID`, `CTRADER_FIX_TRADE_SENDER_SUB_ID`, `CTRADER_FIX_TRADE_TARGET_SUB_ID` are all present and set to `<BROKER_ISSUED_VALUE>`.
- [ ] `REAL_COPY_EXECUTION_ENABLED=false` exactly.
- [ ] No live IP, login, FIX account, SenderCompID, host, egress, or proxy endpoint.
- [ ] Every password / key slot is `<SECRET>`.
- [ ] Plan-group values are `<PLAN_GROUP_PATH>` and commented as **not** the fetch filter.
- [ ] `VITE_*` contains no secret and no connection string.
- [ ] Postgres / Redis password slots are `<SECRET>`.
- [ ] Product source is unchanged by the example-file edit (config template only).

---

## 13. Honesty close

Architecture §56 as printed in v2 is **not** placeholder-only: it repeats live Achiever / StarwaveFX / Pepperstone-cTrader identifiers and only masks passwords and SubIDs. The §55 sentence is stricter than the §56 listing. This report follows the **stricter** rule and the explicit task: placeholders only, SubIDs configurable, real copy **false**.

`D:\Prop\.env.example` already exists and already sets `REAL_COPY_EXECUTION_ENABLED=false` and configurable SubID keys, but it is **not** secret-safe under §55 because it copies those live identifiers. Product workers still do not read any of this.

**Product source was not modified.** This file is the only output.
