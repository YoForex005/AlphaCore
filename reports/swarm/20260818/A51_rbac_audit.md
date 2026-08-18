# A51 — Authentication, RBAC, and `audit_logs` Schema

| Field | Value |
|---|---|
| Agent | A51 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A51_rbac_audit.md` |
| Authority | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§59** |
| Supporting authority | §§40, 41, 45, 46, 53, 55–57, 68, 70, 71, 72.5, 72.19; A06 API contracts; A23 kill-switch / permission split |
| Product source modified | **No** |
| Classification (§73-B) | Identity + RBAC + `audit_logs` = **MISSING** |

This is a design specification only. It does not implement auth, policies, or migrations.

---

## 1. Verdict

Architecture §59 names four dashboard roles and requires that a closed set of privileged mutations be role-gated **and** audited. Table `audit_logs` is listed in §45 and still has no entity, migration, or writer (A01, A03). `apps/api` is anonymous weatherforecast (A06). There is no `users` table, no claims, no policies, no step-up confirmation, and no append-only audit trail.

Honest measured state:

| Control | Status |
|---|---|
| Roles `SuperAdmin`, `RiskManager`, `Analyst`, `ReadOnly` | **MISSING** (named only in §59 / A06) |
| Authentication | **MISSING** |
| Authorization policies | **MISSING** |
| `users` / sessions / lockout | **MISSING** (not in §45; required by §59) |
| `audit_logs` table / entity | **MISSING** |
| Manual-override audit (§72.19) | **MISSING** |
| Secret-safe audit payloads (§55, §57) | **MISSING** (no leak yet — no writer) |
| Public self-registration | correctly **absent** |

This document freezes:

1. Role semantics and hierarchy.
2. Permission catalog and HTTP policy matrix (aligned with A06).
3. Supporting identity tables (minimal, not a second product).
4. `audit_logs` physical schema, immutability, redaction, and query rules.
5. What is in first useful version (§69) vs later phases.

---

## 2. Binding rules (do not weaken)

From §59, §40, §55–57, §72:

1. Only authorized roles may perform the eight privileged mutations listed in §59.
2. **All** of those mutations **must** be audited. §72.19: every **manual override** must be audited.
3. `STOP_NEW_EXECUTION` and `EMERGENCY_FLATTEN` are **two controls, two permissions, two effects** (§40, A23 §8). Do not conflate.
4. Never expose MT5 / proxy / cTrader / FIX / database / Redis passwords to React or to `audit_logs` payloads (§55, §57).
5. Never log authentication tags containing passwords (§57).
6. No public register in v1 (A06). Seed the first `SuperAdmin` out of band.
7. Schema changes only via versioned migrations (§72.3). This file is the contract; it is not a hand-applied SQL script in production.
8. `REAL_COPY_EXECUTION_ENABLED` defaults **false** (§41). Enabling it is `SuperAdmin` + step-up and is **out of first useful version**.
9. Automated model self-promotion is forbidden (§71). Human promote is `SuperAdmin` only and is **out of first useful version**.
10. Broker / FIX credentials are **not** written from React (A06). If an API ever exists, it is `SuperAdmin` + step-up and still never echoes secrets.

---

## 3. Role model

### 3.1 Canonical names

Store and emit **exactly** these strings. Do not invent synonyms (`Admin`, `Viewer`, `Ops`, `Risk`).

```text
SuperAdmin
RiskManager
Analyst
ReadOnly
```

ASP.NET claim: `ClaimTypes.Role` (or `role`) = one of the four.

Policy names (Authorization):

| Policy | Rule |
|---|---|
| `Authenticated` | any of the four roles |
| `ReadOnlyPlus` | any authenticated role |
| `AnalystPlus` | `Analyst` **or** `RiskManager` **or** `SuperAdmin` |
| `RiskManagerPlus` | `RiskManager` **or** `SuperAdmin` |
| `SuperAdminOnly` | `SuperAdmin` |
| `AuditReader` | `RiskManager` **or** `SuperAdmin` |

`ReadOnly` is a real role, not “unauthenticated.” Unauthenticated `/api/**` → **401**. Authenticated but wrong role → **403**.

### 3.2 One primary role per user (v1)

v1 assigns **exactly one** role per user (`users.role`). No multi-role soup, no custom permission editor, no per-broker ACL.

Rationale: four operators on an internal dashboard; §71 prefers simple systems. If a person needs two hats, give them two accounts or promote them.

Future (not now): `user_roles` join only if a measured need appears.

### 3.3 Hierarchy (superset)

```text
SuperAdmin  ⊇  RiskManager  ⊇  Analyst  ⊇  ReadOnly
```

Every permission granted to a lower role is granted to all higher roles, **except** visibility of some audit rows (see §8.5). SuperAdmin-only actions are **not** inherited downward.

### 3.4 Role intent

| Role | Job | Typical humans |
|---|---|---|
| `ReadOnly` | Watch venues, traders, shadow, health. Cannot change system state. Cannot read audit. | observers, investors, junior ops shadowing |
| `Analyst` | Same reads + research hygiene: enable/disable a group for analysis. No money, no copy state, no kill switch, no audit. | researchers, scoring reviewers |
| `RiskManager` | Operate the copy/risk surface: trader lifecycle (except live enable), risk limits, `STOP_NEW_EXECUTION`, read audit of operational actions. Cannot flatten, cannot turn live execution on, cannot change FIX/MT5 secrets, cannot promote models, cannot mint SuperAdmins. | desk / risk |
| `SuperAdmin` | Break-glass + platform: everything RiskManager can do, plus identity admin, symbol-map writes, model promote, broker/FIX config (not via React), `REAL_COPY_EXECUTION_ENABLED`, `EMERGENCY_FLATTEN` with step-up. **Cannot** erase or update `audit_logs`. | owners |

Workers and collectors are **not** roles. They use service credentials off the dashboard IdP and write `audit_logs` only when they persist a **durable operator-equivalent state change** (e.g. engine-raised `GLOBAL_STOP` engagement). Routine ingest goes to `system_events`, not `audit_logs` (see §7).

---

## 4. Permission catalog

Stable permission codes. Persist these in `audit_logs.action` (and optionally in policy metadata). Do not rename after first migration.

### 4.1 Privileged mutations required by §59

| Permission code | §59 wording | Min role | Step-up | First useful (§69 / A06) |
|---|---|---|---|---|
| `execution.enable` | enable real execution | SuperAdmin | **yes** | **No** — keep flag false; API 409 |
| `risk.limits.write` | change risk limits | RiskManager | no | Optional write; **read** yes |
| `trader.copy.pause_resume` | pause/resume trader copying | RiskManager | no | **Yes** (Watch / Shadow / Paused / RiskBlocked) |
| `mapping.symbol.write` | change symbol mapping | SuperAdmin | no | Read yes; **write later** |
| `risk.stop_new.activate` | activate stop-new-orders | RiskManager | no | **Yes** (set **and** clear) |
| `risk.flatten.request` | request emergency flatten | SuperAdmin | **yes** + confirm token | **No** |
| `model.promote` | promote a model | SuperAdmin | recommended | **No** (no ML; §71 no auto-promote) |
| `config.broker_fix.write` | change broker/FIX configuration | SuperAdmin | **yes** | **No** — env / secret store, not React |

### 4.2 Additional operator mutations (derived, still audited)

These are not in the §59 bullet list but are manual overrides (§72.19) or A06 first-useful writes.

| Permission code | Action | Min role | First useful |
|---|---|---|---|
| `group.analysis.toggle` | `PATCH` `enabledForAnalysis` | Analyst | **Yes** |
| `trader.state.write` | `PATCH` trader lifecycle (same family as pause/resume; includes Shadow select) | RiskManager | **Yes** |
| `trader.live.promote` | set state `LIVE` / `LIVE_CANDIDATE` while execution may send | SuperAdmin | **No** while flag false → 409 |
| `allocation.write` | change live/shadow allocation | RiskManager | later |
| `identity.user.create` | create user (out of band / admin API) | SuperAdmin | seed path yes; public no |
| `identity.user.role.write` | change another user's role | SuperAdmin | when identity API exists |
| `identity.user.disable` | disable / lock user | SuperAdmin | when identity API exists |
| `identity.user.unlock` | clear lockout | SuperAdmin | when identity API exists |
| `auth.password.reset` | admin-initiated reset | SuperAdmin | later |
| `auth.session.revoke` | revoke one or all sessions | self or SuperAdmin | login exists |
| `settings.public.write` | non-secret operational flags except execution enable | SuperAdmin | later |

### 4.3 Reads (not all audited)

| Permission code | Min role | Notes |
|---|---|---|
| `dash.read` | ReadOnly | Overview, brokers, groups, traders, trades, scores, shadow, FIX cards, risk snapshot, reconciliation, system health, public settings |
| `audit.read` | RiskManager | `GET /api/v1/audit/logs`. ReadOnly and Analyst → **403** |
| `audit.read.all` | SuperAdmin | includes identity-admin and secret-adjacent config rows |
| `identity.user.read` | SuperAdmin | user list; never password hashes |

Successful **reads are not written** to `audit_logs` in v1 (volume). Exceptions that **are** written:

- `audit.read` of a **single** high-sensitivity row is not required in v1; list access is itself role-gated.
- Export of audit (if ever added) is `audit.export` / SuperAdmin and **is** audited.

Failed authorization (403) on a privileged mutation **is** audited (`outcome = denied`). Failed login is audited without the password (`auth.login.fail`).

---

## 5. Role × capability matrix

Legend: **Y** allowed · **N** denied (403 if authenticated) · **—** endpoint absent / 409 in that phase.

### 5.1 Dashboard / API (aligns with A06 §6 and expands it)

| Capability | ReadOnly | Analyst | RiskManager | SuperAdmin |
|---|---|---|---|---|
| `GET` operational dashboards (`dash.read`) | Y | Y | Y | Y |
| `GET /api/v1/settings/public` | Y | Y | Y | Y |
| `GET /api/v1/auth/me` | Y | Y | Y | Y |
| `POST /api/v1/auth/logout` | Y | Y | Y | Y |
| `PATCH /api/v1/mt5/groups/{id}` analysis toggle | N | Y | Y | Y |
| `PATCH /api/v1/traders/{id}/state` Watch/Shadow/Paused/RiskBlocked | N | N | Y | Y |
| `PATCH` trader → `LIVE` / `LIVE_CANDIDATE` while `REAL_COPY_EXECUTION_ENABLED=false` | N | N | N | **409** |
| `POST /api/v1/risk/stop-new-execution` set/clear | N | N | Y | Y |
| `GET /api/v1/risk/limits` (non-secret numeric policy) | Y | Y | Y | Y |
| `PUT /api/v1/risk/limits` | N | N | Y | Y |
| `GET /api/v1/audit/logs` | N | N | Y | Y |
| `GET` identity / user admin | N | N | N | Y |
| `POST /api/v1/risk/emergency-flatten` | N | N | N | later (v1: 404/409) |
| `POST /api/v1/execution/enable` | N | N | N | later (v1: 409) |
| `POST /api/v1/models/{id}/promote` | N | N | N | later (v1: 404) |
| `PUT` broker/FIX credentials | N | N | N | **never from React** |
| Swagger UI in production | N | N | N | only if locked or disabled |
| Erase / update `audit_logs` | N | N | N | **N** |

Unauthenticated: only `POST /api/v1/auth/login`, `POST /api/v1/auth/refresh` (refresh cookie/token), `GET /health`, and optionally `GET /health/ready`. Everything else 401.

### 5.2 §59 privileged-action matrix (authoritative)

| Action | ReadOnly | Analyst | RiskManager | SuperAdmin |
|---|---|---|---|---|
| Enable real execution | N | N | N | Y + step-up |
| Change risk limits | N | N | Y | Y |
| Pause / resume trader copying (incl. shadow select) | N | N | Y | Y |
| Change symbol mapping | N | N | N | Y |
| Activate / clear `STOP_NEW_EXECUTION` | N | N | Y | Y |
| Request `EMERGENCY_FLATTEN` | N | N | N | Y + step-up + confirm token |
| Promote a model | N | N | N | Y |
| Change broker / FIX configuration | N | N | N | Y (not via React) |

### 5.3 Nav visibility (React, §46)

Hide write buttons the role cannot use. **Do not** hide read pages from ReadOnly except:

| Page | ReadOnly | Analyst | RiskManager | SuperAdmin |
|---|---|---|---|---|
| Overview … System Health (all §46 except Audit / Settings writes) | show | show | show | show |
| Audit | hide | hide | show | show |
| Settings (flags, no secrets) | show read | show read | show read | show read + identity |
| Models promote control | hide | hide | hide | show when Phase 6 exists |
| Live Copy / Flatten controls | show **read-only** state | same | stop-new only | flatten/enable when phase allows |

UI hiding is not security. API policies are.

---

## 6. Identity (supporting schema; not in §45)

§45 does not name `users`. §59 cannot be implemented without an actor. These tables are **in scope for the RBAC design** and should be added in the same migration family as `audit_logs`. They are not a full IdP product.

### 6.1 `users`

```sql
CREATE TABLE users (
    id                  uuid PRIMARY KEY,
    username            text NOT NULL,
    display_name        text NOT NULL,
    email               text NULL,
    role                text NOT NULL,
    password_hash       text NOT NULL,
    password_alg        text NOT NULL DEFAULT 'argon2id',
    is_enabled          boolean NOT NULL DEFAULT true,
    is_seed             boolean NOT NULL DEFAULT false,
    failed_login_count  integer NOT NULL DEFAULT 0,
    lockout_until       timestamptz NULL,
    last_login_at       timestamptz NULL,
    created_at          timestamptz NOT NULL,
    updated_at          timestamptz NOT NULL,
    disabled_at         timestamptz NULL,
    disabled_by         uuid NULL REFERENCES users (id),
    CONSTRAINT users_username_nlc UNIQUE (username),
    CONSTRAINT users_role_chk CHECK (role IN ('SuperAdmin', 'RiskManager', 'Analyst', 'ReadOnly')),
    CONSTRAINT users_username_chk CHECK (username ~ '^[a-zA-Z0-9._-]{3,64}$')
);

CREATE INDEX users_role_idx ON users (role);
CREATE INDEX users_enabled_idx ON users (is_enabled);
```

Rules:

- `username` is the login key. Case-preserving, compare case-insensitive in application (`citext` acceptable instead of `text` + unique).
- **No** self-service register endpoint.
- First SuperAdmin: seeded by a versioned migration or an offline CLI that reads the password from the OS secret store / user-secrets, never from a committed file. `is_seed = true`.
- There must always be **≥1 enabled SuperAdmin**. Disabling or demoting the last SuperAdmin is rejected and audited (`outcome = failed`).
- `password_hash` is **never** selected into any DTO, SignalR payload, or `audit_logs.before_json` / `after_json`.
- SuperAdmin cannot assign `SuperAdmin` to a new user without step-up (when that API exists).

### 6.2 `user_sessions`

Refresh/session store so logout and admin revoke work.

```sql
CREATE TABLE user_sessions (
    id                  uuid PRIMARY KEY,
    user_id             uuid NOT NULL REFERENCES users (id),
    refresh_token_hash  text NOT NULL,
    created_at          timestamptz NOT NULL,
    expires_at          timestamptz NOT NULL,
    revoked_at          timestamptz NULL,
    revoked_reason      text NULL,
    last_seen_at        timestamptz NULL,
    client_ip           inet NULL,
    user_agent          text NULL
);

CREATE INDEX user_sessions_user_idx ON user_sessions (user_id, expires_at DESC);
CREATE UNIQUE INDEX user_sessions_refresh_hash_uidx ON user_sessions (refresh_token_hash);
```

Suggested lifetimes (config, not code constants — same spirit as A23 thresholds):

| Token | Default | Notes |
|---|---|---|
| Access JWT | 15 minutes | roles baked in; change-role requires re-login or short TTL |
| Refresh | 12 hours absolute | rotate on each refresh; reuse of an old refresh revokes the family |
| Lockout | 15 minutes after 8 failures | increment on `auth.login.fail` |

### 6.3 `step_up_challenges`

Required for `execution.enable` and `risk.flatten.request`. Reusable later for credential writes and SuperAdmin grants.

```sql
CREATE TABLE step_up_challenges (
    id                  uuid PRIMARY KEY,
    user_id             uuid NOT NULL REFERENCES users (id),
    purpose             text NOT NULL,
    code_hash           text NOT NULL,
    created_at          timestamptz NOT NULL,
    expires_at          timestamptz NOT NULL,
    consumed_at         timestamptz NULL,
    consumed_by_action  text NULL,
    CONSTRAINT step_up_purpose_chk CHECK (purpose IN (
        'execution.enable',
        'risk.flatten.request',
        'config.broker_fix.write',
        'identity.user.role.write',
        'model.promote'
    ))
);

CREATE INDEX step_up_user_purpose_idx ON step_up_challenges (user_id, purpose, expires_at DESC);
```

Flow:

1. Authenticated SuperAdmin `POST /api/v1/auth/step-up` with password re-entry (or TOTP later).
2. Server stores only `code_hash`, returns `{ challengeId, expiresAt }` (not the raw code if using a one-time code; if password-reentry-only, issue a short-lived challenge id).
3. Mutation includes `X-Step-Up-Challenge: {id}` or body `confirmToken`.
4. Challenge is single-use, purpose-bound, ≤ 5 minutes.
5. Audit row sets `step_up_confirmed = true` and `step_up_challenge_id`.

v1 first-useful mutations (`stop-new`, trader state, group analysis) do **not** require step-up.

### 6.4 What identity is **not**

- No OAuth social login.
- No public “forgot password” over the internet in v1 (out-of-band SuperAdmin reset).
- No per-broker tenancy.
- No storing JWT access tokens in Postgres.
- No Active Directory / SSO required for first useful. Cookie BFF or bearer JWT are both acceptable (A06). Pick one in implementation; this spec is vendor-neutral.

Recommended host shape when implemented (not done here):

```text
apps/api/Auth/          # login, refresh, policies
Domain/Enums/UserRole.cs
Domain/Entities/User.cs
Domain/Entities/AuditLog.cs
Infrastructure/Persistence/  # EF + migrations
```

---

## 7. What belongs in `audit_logs` vs sibling tables

Do **not** overload `audit_logs`.

| Table | Writer | Contents | Mutable? |
|---|---|---|---|
| `audit_logs` | API command handlers + rare worker “engage durable kill switch” | **Who** changed **what** operator-controlled state; denied privileged attempts; login success/fail | **Append-only** |
| `system_events` | collectors, FIX adapter, workers | ingest lag, reconnect, session drop, backfill — not a human decision | append |
| `risk_decisions` / `risk_events` | risk engine | approve / reduce / reject / pause / global stop **on a CopyIntent** | append (engine SoT) |
| `outbox_events` | same transaction as raw persist | async work, not an operator trail | processed |
| `fix_session_events` | FIX adapter | protocol session facts | append |

If the **engine** requests `GLOBAL_STOP` and a **durable kill-switch row** is flipped, write **both**: `risk_events` (why the engine asked) and `audit_logs` with `actor_kind = 'system'`, `action = 'risk.stop_new.activate'`, `reason` = engine code (A23 §8.3).

Shadow fills, deals, quotes — **never** `audit_logs`.

---

## 8. `audit_logs` schema

### 8.1 Physical table (PostgreSQL)

```sql
CREATE TABLE audit_logs (
    id                      uuid PRIMARY KEY,
    occurred_at             timestamptz NOT NULL,
    actor_kind              text NOT NULL,
    actor_user_id           uuid NULL REFERENCES users (id),
    actor_username          text NOT NULL,
    actor_role              text NOT NULL,
    action                  text NOT NULL,
    action_category         text NOT NULL,
    outcome                 text NOT NULL,
    entity_type             text NOT NULL,
    entity_id               text NOT NULL,
    broker_id               uuid NULL,
    source_login            text NULL,
    correlation_id          uuid NOT NULL,
    request_id              uuid NULL,
    idempotency_key         text NULL,
    http_method             text NULL,
    http_path               text NULL,
    http_status             integer NULL,
    client_ip               inet NULL,
    user_agent              text NULL,
    reason                  text NULL,
    step_up_confirmed       boolean NOT NULL DEFAULT false,
    step_up_challenge_id    uuid NULL,
    before_json             jsonb NULL,
    after_json              jsonb NULL,
    metadata_json           jsonb NOT NULL DEFAULT '{}'::jsonb,
    CONSTRAINT audit_actor_kind_chk CHECK (actor_kind IN ('user', 'system', 'worker')),
    CONSTRAINT audit_actor_role_chk CHECK (actor_role IN (
        'SuperAdmin', 'RiskManager', 'Analyst', 'ReadOnly', 'system', 'worker'
    )),
    CONSTRAINT audit_outcome_chk CHECK (outcome IN ('succeeded', 'failed', 'denied')),
    CONSTRAINT audit_category_chk CHECK (action_category IN (
        'auth', 'identity', 'trader', 'group', 'risk', 'execution',
        'mapping', 'model', 'config', 'settings', 'audit'
    )),
    CONSTRAINT audit_actor_user_chk CHECK (
        (actor_kind = 'user' AND actor_user_id IS NOT NULL)
        OR (actor_kind IN ('system', 'worker') AND actor_user_id IS NULL)
    )
);

CREATE INDEX audit_logs_occurred_idx
    ON audit_logs (occurred_at DESC);

CREATE INDEX audit_logs_actor_idx
    ON audit_logs (actor_user_id, occurred_at DESC);

CREATE INDEX audit_logs_action_idx
    ON audit_logs (action, occurred_at DESC);

CREATE INDEX audit_logs_entity_idx
    ON audit_logs (entity_type, entity_id, occurred_at DESC);

CREATE INDEX audit_logs_correlation_idx
    ON audit_logs (correlation_id);

CREATE INDEX audit_logs_category_idx
    ON audit_logs (action_category, occurred_at DESC);

CREATE UNIQUE INDEX audit_logs_idempotency_uidx
    ON audit_logs (idempotency_key)
    WHERE idempotency_key IS NOT NULL;
```

Optional later (not v1): `PARTITION BY RANGE (occurred_at)` monthly. Do not add hash-chain columns until a compliance need is measured.

### 8.2 Column contract

| Column | Required | Meaning |
|---|---|---|
| `id` | yes | New UUID per row. Never reuse. |
| `occurred_at` | yes | UTC instant of the decision, **not** DB default “now” if the handler already has a clock. Application sets it. |
| `actor_kind` | yes | `user` / `system` / `worker`. |
| `actor_user_id` | user only | FK to `users`. Frozen even if the user is later renamed or disabled. |
| `actor_username` | yes | **Snapshot** at event time (user rename must not rewrite history). System: worker name (`mt5-worker`, `fix-worker`, `risk-engine`). |
| `actor_role` | yes | Role **at event time**. Users: one of the four. System/worker: `system` / `worker`. |
| `action` | yes | Permission code from §4 (`trader.state.write`, `risk.stop_new.activate`, …). |
| `action_category` | yes | Coarse filter for the Audit page. |
| `outcome` | yes | `succeeded` / `failed` (validation, 409, 500 after auth) / `denied` (403). |
| `entity_type` | yes | Stable name: `trader`, `mt5_group`, `risk_limits`, `kill_switch`, `user`, `session`, `source_symbol_mapping`, `model_version`, `broker`, `fix_session`, `execution_flag`. |
| `entity_id` | yes | String form of the entity key. Traders: `{brokerId}:{login}` (A06). Groups: group UUID. Global flags: `global`. |
| `broker_id` | when known | Enables filter by source broker. |
| `source_login` | when trader-scoped | MT5 login as text; never a password. |
| `correlation_id` | yes | Same id as structured logs (§57). Generate at API middleware if missing (`X-Correlation-Id`). |
| `request_id` | when HTTP | Per-request id (may equal correlation or be a child). |
| `idempotency_key` | when client sent one | Prevents double audit + double mutate. Unique when present. |
| `http_method` / `http_path` / `http_status` | when HTTP | Path **template** preferred (`/api/v1/traders/{traderId}/state`), never query strings with tokens. |
| `client_ip` / `user_agent` | when HTTP | Forensic; not shown to Analyst. |
| `reason` | optional | Operator-supplied or engine reason code. No secrets. |
| `step_up_confirmed` / `step_up_challenge_id` | flatten / enable / … | Must be true for those actions on `succeeded`. |
| `before_json` / `after_json` | mutations | Redacted snapshots. Null on deny-before-load if nothing was read. |
| `metadata_json` | yes | Extra non-secret facts (`fromState`, `toState`, `confirmPhraseMatched`). Default `{}`. |

### 8.3 Immutability (mandatory)

`audit_logs` is **insert-only**.

Application:

- No `UPDATE` / `DELETE` methods on the repository.
- Corrections are **new rows** (`action` may be `audit.correction` / SuperAdmin) that reference the original `id` in `metadata_json.corrects_id`. Never rewrite `before_json`.

Database (apply in the same migration as the table):

```sql
REVOKE UPDATE, DELETE, TRUNCATE ON audit_logs FROM PUBLIC;
-- grant INSERT, SELECT to the app role only

CREATE OR REPLACE FUNCTION audit_logs_forbid_mutation()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'audit_logs is append-only';
END;
$$;

CREATE TRIGGER audit_logs_no_update
    BEFORE UPDATE OR DELETE ON audit_logs
    FOR EACH ROW
    EXECUTE FUNCTION audit_logs_forbid_mutation();
```

SuperAdmin has **no** application permission to bypass this. Restoration from backup is an ops procedure outside the API.

Retention: **keep forever in v1**. No purge job. Partition + archive is a later ops decision, still not a DELETE from the app.

### 8.4 Redaction rules for JSON columns

`before_json` and `after_json` are allow-listed snapshots, **not** EF entity dumps.

**Forbidden keys** (drop if present; never store as `***` that still proves the secret length if avoidable — replace with `"[REDACTED]"`):

```text
password
passwordHash
password_hash
mt5Password
fixPassword
proxyPassword
proxyUsername
connectionString
redisPassword
refreshToken
refresh_token_hash
code_hash
SenderSubID          -- if treated as broker-issued secret
TargetSubID          -- same
Authorization
Cookie
```

Also forbid any key matching `(?i)(password|secret|token|connectionstring|privatekey)`.

**Allowed examples**

Trader state change:

```json
{
  "state": "WATCH",
  "updatedAt": "2026-08-18T12:00:00Z"
}
```

```json
{
  "state": "SHADOW",
  "updatedAt": "2026-08-18T12:01:00Z"
}
```

Risk limits: numeric policy fields only (A23). No credentials.

Broker “config changed” (if ever): `{ "displayName": "...", "server": "...", "managerLoginMasked": "20**", "password": "[REDACTED]" }` — better: omit password key entirely and set `metadata_json.secretFieldsRotated = ["MT5_PASSWORD"]`.

Login fail: `{ "username": "alice" }` only. Never the attempted password.

### 8.5 Read policy for `GET /api/v1/audit/logs`

| Caller | Rows returned |
|---|---|
| Anonymous | 401 |
| ReadOnly, Analyst | 403 (no row leak) |
| RiskManager | `action_category IN (trader, group, risk, execution, mapping, model, settings)` **and** `action` not in the identity/secret set below |
| SuperAdmin | all rows |

Hidden from RiskManager (SuperAdmin only):

```text
identity.user.create
identity.user.role.write
identity.user.disable
identity.user.unlock
auth.password.reset
config.broker_fix.write
```

`auth.login.fail` / `auth.login.ok` / `auth.session.revoke`: SuperAdmin only (credential-stuffing privacy). RiskManager does not need them to operate the desk.

Query parameters (A06): `actor`, `action`, `from`, `to`, plus `entityType`, `entityId`, `outcome`, `correlationId`, `cursor`/`page` (max 200). Default sort `occurred_at DESC`.

Response DTO allow-list: the columns in §8.2 **except** nothing extra. Do not add `password_hash` via join. `client_ip` / `user_agent` visible to SuperAdmin; RiskManager may see them on operational rows (desk forensics) — acceptable. Do not show `step_up` code hashes (not stored on this table).

---

## 9. Action catalog (closed list for v1 writers)

Implementations must not invent parallel verbs (`updateTrader` vs `trader.state.write`).

| `action` | `action_category` | `entity_type` | When written |
|---|---|---|---|
| `auth.login.ok` | auth | session | successful login |
| `auth.login.fail` | auth | session | bad password / unknown user (same message to client) |
| `auth.logout` | auth | session | logout |
| `auth.session.revoke` | auth | session | admin or self revoke |
| `auth.step_up.ok` | auth | session | challenge issued/consumed (consumed row on the mutation is enough; optional) |
| `identity.user.create` | identity | user | seed/admin create |
| `identity.user.role.write` | identity | user | role change |
| `identity.user.disable` | identity | user | disable |
| `identity.user.unlock` | identity | user | unlock |
| `auth.password.reset` | identity | user | admin reset (no hash in JSON) |
| `group.analysis.toggle` | group | mt5_group | PATCH enabledForAnalysis |
| `trader.state.write` | trader | trader | PATCH state (non-live) |
| `trader.copy.pause_resume` | trader | trader | alias only if pause is a dedicated route; prefer `trader.state.write` |
| `trader.live.promote` | trader | trader | state → LIVE* |
| `risk.limits.write` | risk | risk_limits | PUT limits document |
| `risk.stop_new.activate` | risk | kill_switch | set or clear `STOP_NEW_EXECUTION` (`after_json.stopNewExecution` bool) |
| `risk.flatten.request` | risk | kill_switch | flatten requested / completed / failed |
| `execution.enable` | execution | execution_flag | set `REAL_COPY_EXECUTION_ENABLED` |
| `mapping.symbol.write` | mapping | source_symbol_mapping | mapping change |
| `model.promote` | model | model_version | human promote |
| `config.broker_fix.write` | config | broker or fix_session | secret-store rotation recorded |
| `settings.public.write` | settings | settings | non-secret flags |
| `audit.correction` | audit | audit_log | compensating row |

`trader.copy.pause_resume` is the §59 name; persist **`trader.state.write`** as the single code and put `metadata_json.section59 = "pause/resume trader copying"` if a report needs the wording. Do not write two rows for one PATCH.

---

## 10. Mutation write protocol

Every privileged or operator mutation follows the same order:

```text
authenticate → authorize policy → (step-up if required)
    → load entity
    → validate (409 if execution flag / phase forbids)
    → begin transaction
        persist domain change
        insert audit_logs (same transaction)
    → commit
    → return DTO (allow-list)
```

Rules:

1. **Same transaction** as the domain write. A committed state change without an audit row is a defect. An audit row without the state change is a defect.
2. If authorization fails **before** load, still insert `outcome = denied` in a **separate** short transaction (do not hold locks). Include `action`, `entity_id` if present on the URL, `actor_*`, `http_*`.
3. `409` (e.g. promote to LIVE while execution disabled) is `outcome = failed`, not `denied`.
4. Idempotent replay with the same `idempotency_key`: return the original result; **do not** insert a second audit row (unique index).
5. `correlation_id` is required on the audit row and on the structured log line for the same command (§57).
6. Engine-initiated durable kill-switch: `actor_kind = system`, `actor_username = 'risk-engine'`, `actor_role = 'system'`, `reason` = risk reason code.

### 10.1 Required `before` / `after` by action

| Action | `before_json` | `after_json` |
|---|---|---|
| `trader.state.write` | `{ "state": "<old>" }` | `{ "state": "<new>" }` |
| `group.analysis.toggle` | `{ "enabledForAnalysis": bool }` | `{ "enabledForAnalysis": bool }` |
| `risk.stop_new.activate` | `{ "stopNewExecution": bool }` | `{ "stopNewExecution": bool }` |
| `risk.limits.write` | previous numeric document | new numeric document |
| `risk.flatten.request` | `{ "emergencyFlattenActive": bool, "openDestinationPositions": n }` | `{ "emergencyFlattenActive": bool, "requested": true }` |
| `execution.enable` | `{ "realCopyExecutionEnabled": false }` | `{ "realCopyExecutionEnabled": true }` |
| `identity.user.role.write` | `{ "role": "<old>" }` | `{ "role": "<new>" }` |
| `auth.login.fail` | null | null (`metadata_json.username`) |

`LIVE` is **not** a legal `after` state while `REAL_COPY_EXECUTION_ENABLED=false`. Domain rejects; audit `failed`.

---

## 11. Authentication (enough to make RBAC real)

Vendor-neutral. Either cookie BFF (React same-site) or bearer JWT. Requirements that do not depend on vendor:

| Requirement | Rule |
|---|---|
| Login | `POST /api/v1/auth/login` `{ username, password }` → tokens/cookie. Constant-time verify. Generic 401 body. |
| Lockout | After N failures, `lockout_until` set; still generic 401; audit `auth.login.fail`. |
| Me | `GET /api/v1/auth/me` → `{ id, username, displayName, role }` — **one** role string, not a permission dump of secrets. |
| Logout | Revoke refresh family. Audit `auth.logout`. |
| Refresh | Rotate refresh hash. Reuse detection revokes all sessions for that user and audits `auth.session.revoke` / `reason=refresh_reuse`. |
| HTTPS | Required outside local dev. |
| CORS | Explicit Vite origin; no `*` with credentials. |
| CSRF | Required if cookie BFF. |
| Password echo | Never in response, logs, or audit JSON. |
| Register | **Absent.** |

Claims on access token:

```text
sub          = users.id
unique_name  = username
role         = SuperAdmin | RiskManager | Analyst | ReadOnly
jti          = session or token id
```

Do not put permissions for flatten/enable in the token as a shortcut around policies.

---

## 12. ASP.NET policy map (implementation contract)

When `apps/api` is replaced (A06), register:

```text
AddAuthentication (JWT bearer and/or cookie)
AddAuthorization
  Authenticated        → any authenticated
  ReadOnlyPlus         → ReadOnly, Analyst, RiskManager, SuperAdmin
  AnalystPlus          → Analyst, RiskManager, SuperAdmin
  RiskManagerPlus      → RiskManager, SuperAdmin
  SuperAdminOnly       → SuperAdmin
  AuditReader          → RiskManager, SuperAdmin
```

Endpoint binding (first useful + later stubs):

| Endpoint | Policy | Audit on mutate |
|---|---|---|
| `POST /api/v1/auth/login` | anonymous | yes |
| `POST /api/v1/auth/refresh` | refresh | reuse only |
| `POST /api/v1/auth/logout` | Authenticated | yes |
| `GET /api/v1/auth/me` | Authenticated | no |
| `GET /health` | anonymous | no |
| `GET /api/v1/overview` and other `dash.read` | ReadOnlyPlus | no |
| `PATCH /api/v1/mt5/groups/{groupId}` | AnalystPlus | yes |
| `PATCH /api/v1/traders/{traderId}/state` | RiskManagerPlus | yes |
| `POST /api/v1/risk/stop-new-execution` | RiskManagerPlus | yes |
| `PUT /api/v1/risk/limits` | RiskManagerPlus | yes |
| `GET /api/v1/audit/logs` | AuditReader | no |
| `POST /api/v1/risk/emergency-flatten` | SuperAdminOnly + step-up | yes (when exists) |
| `POST /api/v1/execution/enable` | SuperAdminOnly + step-up | yes (when exists) |
| `POST /api/v1/models/{id}/promote` | SuperAdminOnly | yes (when exists) |
| `GET /api/v1/settings/public` | ReadOnlyPlus | no |
| SignalR `OpsHub` | ReadOnlyPlus | no |

Default deny: any new endpoint without a policy is a review fail.

---

## 13. Domain types (when implemented — do not hand-write MQ5; this is C#)

Suggested enums / records (names only; **not** added in this task):

```text
UserRole            SuperAdmin, RiskManager, Analyst, ReadOnly
AuditActorKind      User, System, Worker
AuditOutcome        Succeeded, Failed, Denied
AuditAction         closed set matching §9
AuditCategory       matching check constraint
```

```text
User                id, username, displayName, role, isEnabled, …  (no password on the domain entity if hash stays in persistence)
AuditLog            append-only record matching §8.2
```

Password hash stays in Infrastructure. Domain `User` used for authorization should not carry `PasswordHash`.

`AuditLog` is **not** an EF entity that other aggregates navigate to. No `Trader.AuditLogs` collection.

---

## 14. First useful version vs later

### 14.1 Must exist for first useful (§69 + A06 + §72.19)

```text
[ ] users + one seeded SuperAdmin (out of band)
[ ] login / logout / me / refresh
[ ] four roles + policies on every /api/v1 route
[ ] audit_logs table, append-only trigger, redaction helper
[ ] audited: group.analysis.toggle
[ ] audited: trader.state.write (not LIVE)
[ ] audited: risk.stop_new.activate (set and clear)
[ ] GET /api/v1/audit/logs for RiskManager+ (row filter §8.5)
[ ] 401/403/409 behavior as specified
[ ] no weatherforecast, no anonymous dashboard
```

### 14.2 Explicitly later

```text
[ ] EMERGENCY_FLATTEN endpoint + step-up
[ ] execution.enable + step-up
[ ] model.promote
[ ] symbol mapping write
[ ] broker/FIX config via API
[ ] identity admin API (until then: seed + SQL/CLI)
[ ] TOTP / hardware keys
[ ] audit hash-chain / WORM storage
[ ] monthly partitions
[ ] SSO
```

Kill-switch **test** for go-live (§68) is a Phase 8 concern: `STOP_NEW_EXECUTION` must already be role-gated and audited before anyone tests flatten.

---

## 15. Tests required (§60 spirit)

Unit (no database):

| Test | Expect |
|---|---|
| Policy matrix | each role × each permission code → allow/deny table in §5 |
| Redaction | password / connection string / refresh hash stripped from JSON |
| Hierarchy | Analyst cannot `trader.state.write`; RiskManager cannot `risk.flatten.request` |
| Last SuperAdmin | demote/disable rejected |
| Step-up | flatten/enable without challenge → deny; wrong purpose → deny; expired → deny |
| LIVE while flag false | 409 / `failed`, not a silent write |

Integration:

| Test | Expect |
|---|---|
| Mutation + audit same transaction | crash before commit → no row, no state change |
| Append-only trigger | `UPDATE audit_logs` fails |
| Idempotency key | second POST same key → one audit row |
| Denied PATCH | 403 + `outcome=denied` row |
| Audit GET | ReadOnly 403; RiskManager cannot see `identity.user.role.write` |
| Login fail | no password in `audit_logs` or Serilog |

Do **not** mark RBAC “done” because a `[Authorize]` attribute exists on one controller.

---

## 16. Classification and gaps

| Component | Class |
|---|---|
| §59 role names | specified here; code **MISSING** |
| Auth middleware / policies | **MISSING** |
| `users`, `user_sessions`, `step_up_challenges` | **MISSING** (justified add vs §45) |
| `audit_logs` | **MISSING** (named in §45) |
| Secret redaction in audit | **MISSING** |
| Seed SuperAdmin | **MISSING** |
| A06 weatherforecast anonymous surface | **UNSAFE** until removed |
| This report | design only |

Dependencies (not blockers for writing the spec; blockers for implementation):

- A06 endpoint list (consumed).
- A23 kill-switch split (consumed).
- Infrastructure EF / migrations still empty (A03).
- React Audit page does not exist yet; it must call `GET /api/v1/audit/logs` and never invent rows.

---

## 17. What this document does **not** do

- Does not modify `apps/api`, Domain, Infrastructure, or React.
- Does not choose JWT library vs cookie BFF vendor.
- Does not invent ML promote UX.
- Does not treat `system_events` or `risk_decisions` as a substitute for `audit_logs`.
- Does not allow SuperAdmin to delete history.
- Does not claim RBAC is implemented. It is **not**.

**Bottom line:** four roles in a strict superset (`ReadOnly` ⊂ `Analyst` ⊂ `RiskManager` ⊂ `SuperAdmin`) with SuperAdmin-only break-glass for live enable, flatten, model promote, secret config, and identity. `audit_logs` is an append-only, redacted, correlation-id’d table written in the **same transaction** as every operator mutation. ReadOnly and Analyst cannot read it. Nobody can rewrite it. First useful version audits group analysis, trader Watch/Shadow/Pause, and `STOP_NEW_EXECUTION` only.
