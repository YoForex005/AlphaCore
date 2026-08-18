# B40 — `.gitignore` + `.env.example` (secrets not committed)

| Field | Value |
|---|---|
| Agent | B40 (repo hygiene / secret-commit confirmation) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\B40_gitignore_env.md` |
| Product source edited | **No** |
| Scope | Read `D:\Prop\.gitignore` and `D:\Prop\.env.example`; confirm live secrets are not in Git |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§55–56 |
| Siblings | A19 secrets scan, A75 env example, A103 gitignore, A65 compose, A76 log redaction |
| Repo | `D:\Prop` is git (`main...origin/main`). `mt5-sdk` is a nested repo / parent gitlink `160000 a8f3fe85bc0adf109acb5ec72ed8adb2c0a289df` |

This is a **read-only confirmation**. It does not rewrite `.gitignore`, `.env.example`, `appsettings.json`, or any product C#.

---

## 0. Verdict

| Question | Result |
|---|---|
| Live password / API key / encryption key / connection-string credential **committed**? | **NONE** |
| Live `.env` / `secrets.json` / `*.pfx` ever added to parent git history? | **NONE** (`git rev-list --all -- .env` empty) |
| Real `.env` or `secrets.json` on disk under `D:\Prop`? | **NONE** (only `.env.example` files) |
| Does root `.gitignore` ignore `.env` and keep `.env.example`? | **YES** — lines 28–30 |
| Are secret *slots* in `.env.example` placeholders only? | **YES** — `<SECRET>`, `<BROKER_ISSUED_VALUE>`, `<BASE64_ENCODED_256BIT_KEY>` |
| Are live **venue identifiers** committed? | **YES — FLAG** (hosts, manager logins, FIX account `1369850`, SenderCompID). Not passwords. |
| Working-tree risk (not yet committed)? | `apps/api/appsettings.json` now has empty `Password` + live host/account. Untracked `docker-compose.yml` has `POSTGRES_PASSWORD=ti_dev_only`. |

**Overall:** **PASS** on the assigned question — **secrets are not committed.**  
**Not identifier-clean.** Root `.env.example` is a copy of architecture §56 (live targeting + secret placeholders), not the A75 placeholder-only sheet.

| Class | Committed? | Notes |
|---|---|---|
| MT5 / proxy / FIX / DB passwords | **No** | Only `<SECRET>` in tracked `.env.example` |
| `MT5_PASSWORD_ENCRYPTION_KEY` | **No** | Placeholder token only |
| User-secrets store | **No** | IDs exist on workers; `%APPDATA%\Microsoft\UserSecrets\<id>` **missing** on this machine |
| Postgres compose password `ti_dev_only` | **No** (file untracked) | Would become a committed local-dev secret if `docker-compose.yml` is added as-is |
| Achiever IP `57.128.141.65`, login `2027` | **Yes** | `.env.example` (HEAD = disk) |
| StarwaveFX IP `84.201.6.142`, login `9904` | **Yes** | `.env.example` |
| cTrader host, account `1369850`, `live.pepperstone.1369850` | **Yes** | `.env.example` + compiled defaults in `CTraderFixOptions` |

No P0 (committed live secret). Identifier leak is P2 reconnaissance, same class as A19-02 / A75 §3.

---

## 1. Method

Measured on 2026-08-18. Product C# / JSON was **not** modified.

- Read `D:\Prop\.gitignore` (73 lines, 1107 bytes) and `D:\Prop\.env.example` (115 lines, 3408 bytes) in full.
- SHA-256 (disk = HEAD for both files):

  | File | SHA-256 |
  |---|---|
  | `D:\Prop\.gitignore` | `FAE817C1C2F9AD9BEA4353D89A82ED015585A449FC1339561F2C966A0C2B21E0` |
  | `D:\Prop\.env.example` | `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA` |
  | `D:\Prop\mt5-sdk\.gitignore` | `06D08A304754CE6801C2413C1C05373DA90AAD6803C2F0D16E4BAA4028A67F87` |
  | `D:\Prop\mt5-sdk\.env.example` | `937F7CB0A6912A05BEE0E5B672C696D6D4B41F63FFD530D2451C56715020C47C` |

- `git ls-files` / `git ls-files -s` / `git show HEAD:` for `.gitignore`, `.env.example`, `appsettings*.json`, `launchSettings.json`.
- `git check-ignore -v --no-index` on representative secret / store / log paths.
- `git grep --cached` for `PASSWORD=` and live IDs (index = last commit for these files; `.env.example` / `.gitignore` have **no** uncommitted diff).
- `git log --all -- .env` and `git rev-list --all -- .env` (never existed).
- `Get-ChildItem -Force -Recurse` for `.env*`, `secrets.json`, `*.pfx`, `*.pem`, `*.key` — only the two `.env.example` files.
- Read `appsettings.json` (HEAD + worktree), all `appsettings.Development.json`, three `launchSettings.json`, `docker-compose.yml`, `CTraderFixOptions.cs`, `Mt5BrokerOptions.cs`, `DependencyInjection.cs` connection fallback, worker `UserSecretsId`s.
- Compared `.env.example` to architecture §56 and A75 placeholder law.

A19’s “`D:\Prop` has **no** `.git`” is **obsolete**. Parent repo exists; `.gitignore` and `.env.example` landed in `6c41447` (Initial commit).

---

## 2. `.gitignore` (root) — what is ignored

Complete tracked file (`blob f4c0070786c9de4b57a9a86e79b88d43a76a6f18`):

```gitignore
# =========================
# .NET
# =========================
bin/
obj/
out/
build/
.vs/
.idea/
*.user
*.suo
*.userosscache
*.sln.docstates
[Dd]ebug/
[Rr]elease/
artifacts/
TestResults/
coverage/
*.pdb
*.log

# DLLs (vendor SDK tracked deliberately)
# *.dll

# =========================
# Secrets & Environment
# =========================
.env
.env.*
!.env.example
*.pfx
secrets.json
appsettings.Development.json
appsettings.*.local.json

# =========================
# FIX session state
# =========================
store/
fix-store/
*.seqnums
*.session

# =========================
# Node / React
# =========================
node_modules/
dist/
apps/web/.vite/

# =========================
# Python
# =========================
services/ml-service/.venv/
services/ml-service/__pycache__/
*.pyc

# =========================
# Logs
# =========================
logs/

# =========================
# OS
# =========================
Thumbs.db
Desktop.ini
.DS_Store

# =========================
# Vendor SDK — tracked deliberately
# =========================
# vendor/MetaTrader5SDK/Libs/ is committed on purpose
```

### 2.1 Measured `check-ignore`

| Path | Ignored? | Rule |
|---|---|---|
| `.env` | yes | `.gitignore:28:.env` |
| `.env.local` / `.env.production` | yes | `.env.*` |
| `.env.example` | **not** (correct) | `!.env.example` |
| `apps/web/.env` | yes | `.env` |
| `apps/web/.env.development.local` | yes | `.env.*` |
| `apps/web/.env.production` | yes | `.env.*` |
| `secrets.json` | yes | `secrets.json` |
| `apps/api/appsettings.Development.json` | yes (`!!` in `git status --ignored`) | `appsettings.Development.json` |
| `apps/{fix,mt5}-worker/appsettings.Development.json` | yes | same |
| `store/foo`, `fix-store/foo` | yes | `store/`, `fix-store/` |
| `*.seqnums`, `*.session` | yes | basename rules |
| `logs/app.log`, `app.log` | yes | `logs/` + `*.log` |
| `docker-compose.override.yml` | **no** | gap |
| `.envrc` | **no** | gap |
| `apps/fix-worker/log/quote` | **no** | singular `log/` open |
| `foo.body` / QuickFIX `*.header` | **no** | gap if FileStorePath is CWD |
| `*.pem` / `*.key` / `*.p12` | **no** | only `*.pfx` |

`appsettings.json` (non-Development) is **tracked** — correct. Operators must not paste passwords there.

### 2.2 Nested `mt5-sdk`

Parent records `mt5-sdk` as mode `160000` (gitlink). Parent ignore rules **do not** apply inside that repo. Nested `.gitignore` already ignores `.env` / `.env.*` and un-ignores `.env.example`. Nested tracked env file is **only** `.env.example`. SDK example uses `replace_with_manager_password` / empty `DB_PASSWORD=` / empty encryption key — still placeholders, not live secrets.

---

## 3. `.env.example` — secret slots vs identifiers

File is tracked (`blob b71480a8d9f0cd30166c25e1d124ab744a08fa2f`). Disk matches HEAD (`git diff -- .env.example` empty). First line: `Copy to .env and fill secrets locally. Never commit real passwords.`

### 3.1 Secret slots (PASS)

| Key | Committed value | Class |
|---|---|---|
| `MT5_PASSWORD` | `<SECRET>` | placeholder |
| `ACHIEVER_PROXY_USERNAME` | `<SECRET>` | placeholder |
| `ACHIEVER_PROXY_PASSWORD` | `<SECRET>` | placeholder |
| `MT5_STARWAVEFX_PASSWORD` | `<SECRET>` | placeholder |
| `CTRADER_FIX_PASSWORD` | `<SECRET>` | placeholder |
| `CTRADER_FIX_{QUOTE,TRADE}_{SENDER,TARGET}_SUB_ID` | `<BROKER_ISSUED_VALUE>` | placeholder |
| `DATABASE_URL` … `Password=` | `<SECRET>` | placeholder |
| `MT5_PASSWORD_ENCRYPTION_KEY` | `<BASE64_ENCODED_256BIT_KEY>` | placeholder |

No hex/base64 key material. No quoted live password. `REDIS_URL=localhost:6379` has **no** password (A75 wanted `REDIS_PASSWORD=<SECRET>` as a named slot; absence is a catalog gap, not a leak).

`REAL_COPY_EXECUTION_ENABLED=false` is present and correct (architecture §41 / §56 safety floor).

### 3.2 Live identifiers (FLAG — not a secret commit)

These match architecture §56 almost verbatim. Architecture treats them as “non-secret configuration.” A75 treats them as **not safe to publish**.

| Key | Live committed value |
|---|---|
| `MT5_SERVER` | `57.128.141.65` |
| `MT5_LOGIN` | `2027` |
| `MT5_DEFAULT_GROUP` | `demo\Maxmaster` |
| `MT5_SERVER_NAME` | `AchieverGlobalMarkets-Server` |
| `ACHIEVER_EGRESS_IP` | `81.29.145.69` |
| `MT5_STARWAVEFX_SERVER` | `84.201.6.142` |
| `MT5_STARWAVEFX_LOGIN` | `9904` |
| `CTRADER_FIX_HOST` | `live-us-eqx-01.p.c-trader.com` |
| `CTRADER_FIX_ACCOUNT_ID` | `1369850` |
| `CTRADER_FIX_*_SENDER_COMP_ID` | `live.pepperstone.1369850` |
| Plan group paths | `demo\yo-*` / `contest\yo-*` |

Delta vs §56: example sets `ACHIEVER_PROXY_ENABLED=false` and empty proxy host/port; architecture sample has proxy **on** with host `81.29.145.69` port `49527`. Fail-closed example is safer.

Extra blocks **not** in §56: `RISK_*`, `FEATURE_*`, `LOG_*`, `MT5_PASSWORD_ENCRYPTION_KEY`, `DATABASE_URL` / `REDIS_URL`. They do not introduce live secrets.

### 3.3 vs A75 placeholder-only law

A75 §2 forbids live hosts / logins / CompIDs / egress IPs in the committed example. Current file **fails that content law** and **passes §55 “no production secrets in Git.”** B40 does not rewrite the example.

---

## 4. Other committed config (HEAD)

| Path | Tracked? | Secrets? |
|---|---|---|
| `apps/api/appsettings.json` | yes (`10f68b8`) | HEAD = Logging + `AllowedHosts` only |
| `apps/fix-worker/appsettings.json` | yes | Logging only |
| `apps/mt5-worker/appsettings.json` | yes | Logging only |
| `apps/*/appsettings.Development.json` | **no** (ignored) | Logging only on disk |
| `apps/*/Properties/launchSettings.json` | yes | `ASPNETCORE_ENVIRONMENT` / `DOTNET_ENVIRONMENT` only |
| `apps/fix-worker` / `apps/mt5-worker` csproj | yes | `UserSecretsId` GUIDs (not secrets) |
| `apps/api` csproj | yes | **no** `UserSecretsId` |
| `docker-compose.yml` | **no** (untracked) | see §5 |
| React `VITE_*` | client uses `VITE_API_URL` fallback `http://localhost:5000` | no broker/FIX/DB secret |

`git grep --cached` for `PASSWORD=` under `:!reports` `:!*.md` hits **only** `.env.example` placeholders listed in §3.1.

Committed `CTraderFixOptions` defaults (also on HEAD): `Host = live-us-eqx-01.p.c-trader.com`, `SenderCompId = live.pepperstone.1369850`, `Password = string.Empty`, `RealCopyExecutionEnabled = false`. Identifiers, not credentials. `Mt5BrokerOptions.Password` / `ProxyPassword` / `ApiKey` have no literal defaults.

`DependencyInjection` treats a connection string containing `<SECRET>` as “use in-memory DB” — so copying `.env.example` verbatim cannot accidentally connect with a real password.

---

## 5. Working tree (not committed — watch before next commit)

| Path | State | Risk |
|---|---|---|
| `apps/api/appsettings.json` | **modified** | Adds `ConnectionStrings:TraderIntelligence=""` and `CTrader.Password=""`. Live `Host` + `AccountId=1369850`. Empty password is not a secret, but this is the file people will paste a live FIX password into. Prefer env / user-secrets. |
| `docker-compose.yml` | **untracked** | `POSTGRES_PASSWORD: ti_dev_only` — local-dev credential. If committed, it is a known password for a published compose file (acceptable only if labeled local-dev and never reused in prod). |
| `src/Infrastructure/Seeding/DemoSeeder.cs` | **untracked** | Live IPs / logins / CompIDs, **no** password fields. |
| `CTraderFixOptions.cs` | **modified** | Defaults already had live host/CompID on HEAD; password remains empty. |

`git status` does **not** list `.env`, `secrets.json`, or `*.pfx`.

---

## 6. User-secrets / on-disk secret stores

| Store | Present? |
|---|---|
| `D:\Prop\.env` | no |
| `D:\Prop\mt5-sdk\.env` | no |
| Any `secrets.json` under `D:\Prop` | no |
| `%APPDATA%\Microsoft\UserSecrets\dotnet-TraderIntelligence.FixWorker-400770db-…` | **false** |
| `%APPDATA%\Microsoft\UserSecrets\dotnet-TraderIntelligence.Mt5Worker-6850a13e-…` | **false** |
| API `UserSecretsId` | none |

Nothing to leak from a local secrets dump today.

---

## 7. Ignore gaps (hygiene, not current leaks)

Same four holes A103 called; still open; still unused on disk:

1. `docker-compose.override.yml` — typical place to paste host passwords once compose is committed.
2. `.envrc` — direnv would not be ignored.
3. Singular `log/` and QuickFIX `*.body` / `*.header` if FileStore/FileLog land in CWD.
4. `*.pem` / `*.key` / `*.p12` (only `*.pfx` ignored). Non-canonical user-secret copies (`.user-secrets/*.json` other than `secrets.json`).

SDK nested repo still does not ignore `*.log` / `logs/` / `secrets.json`.

None of these gaps currently hide a committed secret, because those files do not exist.

---

## 8. Confirmations (assigned question)

1. **`.gitignore` exists, is tracked, and is the correct §55 shape for env files:** ignore `.env` + `.env.*`, keep `.env.example`. Also ignores `secrets.json`, `appsettings.Development.json`, `*.pfx`.
2. **`.env.example` exists, is the only tracked env file, and contains no live passwords.** Secret values are tokens. `REAL_COPY` is false.
3. **Git history never contained a live `.env` or `secrets.json`.** Initial commit added the example + ignore file; no later secret add.
4. **HEAD `appsettings.json` files contain no connection-string passwords and no FIX/MT5 passwords.**
5. **Caveat (not a secret):** committed `.env.example` + `CTraderFixOptions` publish live venue targeting. Treat as identifier policy (A19/A75), not as a failed secret scan.

**Answer:** secrets are **not** committed. Do not treat the tree as identifier-scrubbed. Do not commit the working-tree `CTrader.Password` slot with a real value, and do not commit compose `POSTGRES_PASSWORD` unless it is an explicit disposable local-dev password.

---

## 9. Findings (report-only; no product edits)

| ID | Sev | Finding |
|---|---|---|
| B40-01 | — | **PASS.** No live MT5 / FIX / proxy / DB / Redis / encryption secret in parent or SDK git. |
| B40-02 | P2 | `.env.example` commits live hosts, manager logins, FIX account, SenderCompID (architecture §56 copy; fails A75 placeholder-only). |
| B40-03 | P3 | Ignore gaps: `docker-compose.override.yml`, `.envrc`, `log/`, `*.pem`/`*.key`/`*.p12`, QuickFIX `*.body`/`*.header`. |
| B40-04 | P3 | Untracked `docker-compose.yml` embeds `POSTGRES_PASSWORD=ti_dev_only`. Decide before first compose commit. |
| B40-05 | P3 | Uncommitted `apps/api/appsettings.json` now has a `CTrader.Password` key (empty). Keep empty or move to env; never commit a filled value. `appsettings.Development.json` is already ignored. |

No I0 rewrite performed. A later increment may apply A75 §4 placeholders and A103 §6 ignore additions without this agent touching product source.
