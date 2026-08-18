# A103 — `.gitignore` for `.env`, user-secrets, FIX store, logs

| Field | Value |
|---|---|
| Agent | A103 (repo hygiene / ignore policy) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A103_gitignore.md` |
| Product source edited | **No** |
| Scope | Scan existing ignore files; recommend patterns for `.env`, user-secrets, QuickFIX FileStore, and logs |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §55 (secrets never in Git; placeholders only in `.env.example`) |
| Siblings | A19 secrets scan, A25 FIX session, A30 I0 hygiene, A35 QuickFIX paths, A50/A76 FileLog ban, A65 compose, A75 `.env.example` |

This file is a **recommendation**. It does not rewrite `D:\Prop\.gitignore` or `D:\Prop\mt5-sdk\.gitignore`. A later I0 increment may copy §6 verbatim.

---

## 0. Verdict

| Question | Result |
|---|---|
| Does a repo-root `.gitignore` exist? | **YES** — `D:\Prop\.gitignore` (1107 bytes, SHA-256 `FAE817C1C2F9AD9BEA4353D89A82ED015585A449FC1339561F2C966A0C2B21E0`, tracked on `main`) |
| Does `mt5-sdk` have its own `.gitignore`? | **YES** — `D:\Prop\mt5-sdk\.gitignore` (482 bytes, SHA-256 `06D08A304754CE6801C2413C1C05373DA90AAD6803C2F0D16E4BAA4028A67F87`) |
| Live `.env` / `secrets.json` / FIX store / product `logs/` on disk? | **NONE** (only tracked `.env.example` files) |
| Are the four requested families already ignored at repo root? | **Mostly.** `.env`, `secrets.json`, `store/`, `fix-store/`, `*.seqnums`, `*.session`, `logs/`, `*.log` are live. Gaps remain. |
| Stale sibling claims | A19-03 / A29 L24 (“no root `.gitignore`”) are **obsolete**. A65 is current: root ignore exists. |

**Overall:** do **not** treat ignore policy as missing. Treat it as **80% done** with four concrete holes that will matter the first day QuickFIX FileStore / FileLog or a copied user-secrets file lands in the tree.

| Family | Root coverage today | Gap |
|---|---|---|
| `.env` | **GOOD** — `.env`, `.env.*`, `!.env.example` | Nested `mt5-sdk` is a **gitlink**; parent rules do not apply inside it. SDK already ignores `.env`. |
| User-secrets | **PARTIAL** — `secrets.json` only | Real store is outside the repo. Local folder copies (`.user-secrets/`, non-`secrets.json` names) are open. API has no `UserSecretsId`. |
| FIX store | **PARTIAL** — `store/`, `fix-store/`, `*.seqnums`, `*.session` | A35 paths `log/quote` + `log/trade` are **open**. QuickFIX `*.body` / `*.header` are **open** if FileStorePath is `.` or the worker CWD. |
| Logs | **GOOD** for `*.log` + `logs/` | Singular `log/` (QuickFIX FileLogPath) is **open**. Serilog `log-*.txt` / `*.clef` not covered. SDK nested repo does **not** ignore `*.log`. |

---

## 1. Method

Read-only scan on 2026-08-18:

- `Get-ChildItem -Force -Recurse` for `.gitignore`, `.env*`, `secrets.json`, `*.seqnums`, `*.session`, `*.pfx`, `store/`, `fix-store/`, `logs/`
- `git -C D:\Prop check-ignore -v --no-index` on representative paths
- `git ls-files` for tracked `appsettings*`, `.env*`, `launchSettings.json`
- `git ls-files -s -- mt5-sdk` (mode `160000` gitlink)
- Worker / API csproj `UserSecretsId`
- `%APPDATA%\Microsoft\UserSecrets` on this machine
- `CTraderFixOptions.cs` (no `FileStorePath` / `FileLogPath` properties yet)
- A30 I0, A35 session settings, A50/A76 FileLog ban, A75 store-path comments

Product C# / JSON / csproj was **not** modified.

---

## 2. What exists on disk

### 2.1 Ignore files

```text
D:\Prop\.gitignore              tracked in parent repo
D:\Prop\mt5-sdk\.gitignore      tracked in nested mt5-sdk repo
```

No `src/.gitignore`, no `apps/*/.gitignore`, no `.dockerignore`, no `docker-compose.yml`.

`D:\Prop` **is** a git repo (`main...origin/main`). `D:\Prop\mt5-sdk` is a **separate** git repo recorded in the parent as a gitlink:

```text
160000 a8f3fe85bc0adf109acb5ec72ed8adb2c0a289df 0  mt5-sdk
```

There is **no** `.gitmodules`. Parent `git check-ignore` therefore does **not** protect files created under `mt5-sdk\`. The nested `.gitignore` is the only ignore law there.

### 2.2 Current root `.gitignore` (complete)

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

### 2.3 Current `mt5-sdk/.gitignore` (secrets + build only)

```gitignore
.env
.env.*
!.env.example
```

plus CMake / MSVC / editor noise. **No** `*.log`, `logs/`, `log/`, `store/`, `secrets.json`, `*.pfx`.

### 2.4 Secret-bearing / store / log files on disk

| Path | Present? | Git |
|---|---|---|
| `D:\Prop\.env` | No | would be ignored |
| `D:\Prop\.env.example` | Yes (3408 B) | **tracked** (correct) |
| `D:\Prop\mt5-sdk\.env` | No | ignored by nested repo |
| `D:\Prop\mt5-sdk\.env.example` | Yes | tracked in nested repo |
| Any `secrets.json` under `D:\Prop` | No | would be ignored at parent |
| `%APPDATA%\Microsoft\UserSecrets\` | **Directory does not exist** | N/A (outside repo) |
| `store/`, `fix-store/`, `log/`, `logs/` (product) | No | — |
| `build-release.log`, `test-release.log` | Yes | ignored (`*.log`) |
| `apps/*/appsettings.Development.json` | Yes (Logging only) | ignored, **not** tracked |
| `apps/*/appsettings.json` | Yes (Logging only) | **tracked** |
| `apps/*/Properties/launchSettings.json` | Yes (localhost + `Development` only) | **tracked** — no credentials |

`git status --ignored` shows `!!` on `appsettings.Development.json`, all `bin/`/`obj/`, and the two root `*.log` files. No ignored `.env` or store directory exists yet.

---

## 3. Family-by-family assessment

### 3.1 `.env`

Root patterns:

```gitignore
.env
.env.*
!.env.example
```

Measured:

| Path | Ignored? |
|---|---|
| `.env` | yes (line 28) |
| `.env.local` / `.env.production` | yes (`.env.*`) |
| `apps/web/.env` | yes |
| `apps/web/.env.development.local` | yes (Vite local overlay) |
| `.env.example` | **not** ignored (line 30 un-ignore) |

This is the correct §55 shape. Keep it.

**Do not weaken** `!.env.example`. Operators copy to `.env`; only the example is committed.

Adjacent notes (not ignore bugs):

- Root `.env.example` currently contains **live venue identifiers** (hosts, manager logins, account `1369850`, SenderCompID). That is an A19/A65/A75 content problem, not an ignore problem. Ignore policy correctly **tracks** the example.
- `.envrc` (direnv) is **open**. Add it if direnv is used.
- `docker-compose.override.yml` is **open**. Compose reads root `.env` automatically (ignored). An override file is the usual place people paste host passwords; ignore it if Compose is added (A65).

### 3.2 User-secrets

**Where they actually live (Windows):**

```text
%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json
```

That path is **outside** `D:\Prop`. Gitignore cannot and need not cover it.

**In-repo IDs (not secrets — keep committed):**

| Project | `UserSecretsId` | Used? | `secrets.json` on this machine |
|---|---|---|---|
| `apps/fix-worker` | `dotnet-TraderIntelligence.FixWorker-400770db-b19b-4432-8d23-92415fa24b79` | unused template | missing |
| `apps/mt5-worker` | `dotnet-TraderIntelligence.Mt5Worker-6850a13e-19ab-4410-9156-a0d5b0d746d1` | unused template | missing |
| `apps/api` | **none** | — | — |

Root already ignores any repo-relative `secrets.json` (including `apps/fix-worker/secrets.json` and `user-secrets/secrets.json`).

**Still open:**

| Path | Why it matters |
|---|---|
| `.user-secrets/fix.json` | operator dump with a non-canonical name |
| `.microsoft/usersecrets/` | Linux-style tree copied into the repo |
| `*.pfx` / `*.p12` / `*.pem` / `*.key` | client TLS / FIX mutual-TLS material (A35). Only `*.pfx` is ignored today |

Recommendation: keep `secrets.json`; add folder aliases and remaining key material. Do **not** ignore `*.csproj` or `UserSecretsId` elements.

Lab store when workers start loading secrets: `dotnet user-secrets set` against the existing worker IDs. Production: process environment / Windows vault — never `appsettings.Production.json`.

### 3.3 FIX store (QuickFIX FileStore)

Product code does **not** yet set `FileStorePath` (`CTraderFixOptions` has host/ports/CompIDs/password only). `FixSimulationHarness` writes no files. The ignore rules are **pre-positioned** for A35 / A75:

```text
FileStorePath=store/quote     # QUOTE
FileStorePath=store/trade     # TRADE   — never share
FileLogPath=log/quote         # A35 sample; A50/A76 forbid this key in product
FileLogPath=log/trade
```

A75 optional env comments: `CTRADER_FIX_QUOTE_FILE_STORE_PATH=store/quote` (same for trade / log).

QuickFIX/n `FileStore` (including the in-tree pin `QuickFix.Net` 1.8.0) writes four files per session id:

```text
{BeginString}-{SenderCompID}-{TargetCompID}[.qualifier].seqnums
{BeginString}-{SenderCompID}-{TargetCompID}[.qualifier].body
{BeginString}-{SenderCompID}-{TargetCompID}[.qualifier].header
{BeginString}-{SenderCompID}-{TargetCompID}[.qualifier].session
```

Measured parent ignore:

| Path | Ignored? | Rule |
|---|---|---|
| `store/quote` / `store/trade` | yes | `store/` |
| `fix-store/quote` | yes | `fix-store/` |
| `apps/fix-worker/store/quote/FIX.4.4-x.seqnums` | yes | `store/` |
| `apps/fix-worker/FIX.4.4-x.seqnums` | yes | `*.seqnums` |
| `apps/fix-worker/FIX.4.4-x.session` | yes | `*.session` |
| `apps/fix-worker/FIX.4.4-x.body` | **NO** | gap |
| `apps/fix-worker/FIX.4.4-x.header` | **NO** | gap |
| `log/quote` / `log/trade` | **NO** | gap (`logs/` is plural only) |

`*.body` / `*.header` are defense-in-depth for a mis-set `FileStorePath=.` (worker CWD). They do **not** collide with C/C++ headers (`.h` / `.hpp`).

A50 / A76: product session factory must **throw** if `FileLogPath` / `ScreenLog` is present (raw tag 554). Ignore `log/` anyway. A developer, the A68 simulator, or a leftover A35 snippet will write `log/quote` the first time someone pastes the sample cfg. FileLog can contain `554=<password>`. Git must not see it.

Never share one store directory between QUOTE and TRADE (A25 / A35 / A56). Ignore policy does not enforce that; session config does. Ignore both trees.

### 3.4 Logs

| Pattern | Covers | Gap |
|---|---|---|
| `*.log` | `build-release.log`, `test-release.log`, `propfirm_backend.log` (C++ `logger.h` default), QuickFIX `*.messages.current.log` | files under **mt5-sdk** CWD are a different repo |
| `logs/` | conventional Serilog / worker directory | not `log/` |
| — | Serilog rolling `logs/log-20260818.txt`, `*.clef` | `log-*.txt` / `*.clef` not listed |
| C++ `PROP_FIRM_LOG_DIR` | `logger.h` writes `{dir}/propfirm_backend.log` | if dir is `logs/` → parent OK; if CWD is `mt5-sdk` → **not** ignored there |

A50 forbids QuickFIX FileLog in product. Ignore remains the last backstop.

Do **not** ignore `reports/` or `docs/`. Swarm artifacts are the permanent store.

---

## 4. `check-ignore` evidence (parent repo)

Ignored (keep):

```text
.env
.env.local
apps/web/.env.development.local
secrets.json
apps/fix-worker/secrets.json
apps/api/appsettings.Development.json
store/quote
fix-store/quote
*.seqnums
*.session
logs/app.log
propfirm_backend.log
build-release.log
```

Open (fix in the next `.gitignore` edit):

```text
log/quote
log/trade
apps/fix-worker/FIX.4.4-sender-target.body
apps/fix-worker/FIX.4.4-sender-target.header
certs/client.pem
certs/client.key
certs/client.p12
.envrc
.user-secrets/fix.json
docker-compose.override.yml
```

---

## 5. What must stay tracked

| Path | Why |
|---|---|
| `.env.example` | §55 placeholder catalog (content is A75’s job) |
| `mt5-sdk/.env.example` | SDK template |
| `apps/*/appsettings.json` | non-secret host config |
| `apps/*/Properties/launchSettings.json` | localhost profiles; no passwords today |
| `*.csproj` `UserSecretsId` | identifier only |
| `reports/` / `docs/` | lab record |
| `mt5-sdk/vendor/MetaTrader5SDK/Libs/*.dll` | tracked deliberately (nested repo) |

**Do not add** a blanket `*.dll` or `*.xml` ignore that would drop the vendor SDK or a future `FIX44-cTrader.xml`.

---

## 6. Recommended root `.gitignore` (apply later — do not apply in this task)

Replace `D:\Prop\.gitignore` with this file, or merge the marked **ADD** blocks. Existing sections are preserved.

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
*.DotSettings.user
[Dd]ebug/
[Rr]elease/
artifacts/
TestResults/
coverage/
*.pdb
*.log
*.clef

# DLLs (vendor SDK tracked deliberately)
# *.dll

# =========================
# Secrets & Environment
# =========================
.env
.env.*
!.env.example
.envrc
*.pfx
*.p12
*.pem
*.key
*.snk
secrets.json
.user-secrets/
user-secrets/
appsettings.Development.json
appsettings.*.local.json

# Compose local overlay (passwords). Keep docker-compose.yml tracked.
docker-compose.override.yml

# =========================
# FIX session state (QuickFIX FileStore)
# Distinct trees for QUOTE vs TRADE — never share sequence files.
# =========================
store/
fix-store/
fixstore/
file-store/
*.seqnums
*.session
*.body
*.header

# =========================
# Logs (including QuickFIX FileLogPath=log/quote|trade)
# Product factory must reject FileLogPath; ignore is the backstop.
# =========================
log/
logs/
fix-log/
log-*.txt

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

Delta vs today’s file (the only reason to touch it):

| Add | Why |
|---|---|
| `log/` `fix-log/` | A35 `FileLogPath=log/quote` and `log/trade` |
| `*.body` `*.header` | QuickFIX FileStore if CWD is used as store |
| `fixstore/` `file-store/` | alternate directory names |
| `.user-secrets/` `user-secrets/` | dumped user-secrets trees |
| `*.p12` `*.pem` `*.key` `*.snk` | TLS / signing material (A35 client cert) |
| `.envrc` | direnv |
| `docker-compose.override.yml` | local Compose secrets |
| `*.clef` `log-*.txt` | Serilog / unstructured leftovers |
| `*.DotSettings.user` | Rider / ReSharper user settings |

---

## 7. Recommended nested `mt5-sdk/.gitignore` additions

Parent rules **do not apply** inside the gitlink. Append:

```gitignore
# Secrets / TLS (parent cannot see this repo)
secrets.json
*.pfx
*.p12
*.pem
*.key

# Logs (spdlog default: ./propfirm_backend.log)
*.log
logs/
log/
PROP_FIRM_LOG_DIR is env-only; still ignore conventional dirs.

# Accidental FileStore if a tool is run from this CWD
store/
*.seqnums
*.session
```

Keep `!.env.example`. Do not ignore `vendor/MetaTrader5SDK/**`.

---

## 8. Policy choices (do not silently flip)

### 8.1 `appsettings.Development.json`

Currently ignored and untracked. On-disk copies are Logging-only (safe).

| Keep ignore | Commit the file |
|---|---|
| Stops the first `ConnectionStrings` + password from landing on `main` | A30 I0 wants shared non-secret compose hostnames in Development |
| Matches A65 | New clones will not have the file; `appsettings.json` already carries the same logging |

**Recommendation:** keep the ignore until Development files are **guaranteed** placeholder-only and reviewed. Prefer secrets in user-secrets / `.env`, not in Development JSON. If I0 later commits Development, drop that one line and keep `appsettings.*.local.json`.

### 8.2 `*.body` / `*.header`

Safe in this tree (no such source files). If a later non-FIX `*.header` document appears, narrow to:

```gitignore
FIX*.body
FIX*.header
*.seqnums
*.session
```

and require `FileStorePath` under `store/` / `fix-store/`.

### 8.3 FileLog vs FileStore

| Kind | Product policy | Gitignore |
|---|---|---|
| FileStore (`store/quote`, `store/trade`) | **Required** (sequence persistence) | ignore the directories |
| FileLog (`log/quote`, `log/trade`) | **Forbidden** in product (A50/A76) | ignore anyway |

---

## 9. Operator checklist (when someone next edits `.gitignore`)

1. Paste §6 into `D:\Prop\.gitignore`. Do not hand-edit product C#.
2. Append §7 to `D:\Prop\mt5-sdk\.gitignore` (nested repo commit).
3. Confirm with:

```powershell
git -C D:\Prop check-ignore -v --no-index -- .env secrets.json store/quote log/quote log/trade apps/fix-worker/FIX.4.4-x.body
git -C D:\Prop ls-files -- .env secrets.json store fix-store logs
```

Expected: first command prints ignore rules; second prints nothing.

4. Never `git add -f` `.env`, `secrets.json`, `store/`, `log/`, or `*.pfx`.
5. If a secret was committed before the ignore existed: rotate the secret; `git rm --cached` is not enough by itself.
6. Add `UserSecretsId` on the API only when the API first reads a secret. Do not copy worker secrets into `appsettings.json`.

---

## 10. Adjacent (out of scope — do not implement here)

| Item | Owner |
|---|---|
| Rewrite root `.env.example` to placeholders only (strip live IPs / login / account / CompID) | A75 / I0 |
| `.dockerignore` (A65 §5.4) | compose increment |
| Wire `AddUserSecrets` on workers; reject `FileLogPath` in session factory | A50 / fix-worker |
| Distinct `FileStorePath` per qualifier | A25 / A35 |

---

## 11. Classification

| Item | Class |
|---|---|
| Root `.gitignore` exists and is tracked | `EXISTS_AND_GOOD` |
| `.env` / `.env.*` / `!.env.example` | `EXISTS_AND_GOOD` |
| `secrets.json` + `*.pfx` | `EXISTS_AND_GOOD` (narrow) |
| `store/` + `fix-store/` + `*.seqnums` + `*.session` | `EXISTS_AND_GOOD` |
| `logs/` + `*.log` | `EXISTS_AND_GOOD` |
| `log/` (A35 FileLogPath) | `MISSING` |
| QuickFIX `*.body` / `*.header` outside `store/` | `MISSING` |
| User-secrets directory aliases + `*.pem`/`*.key`/`*.p12` | `MISSING` |
| `mt5-sdk` ignore of `*.log` / `store/` / `secrets.json` | `MISSING` (nested repo) |
| Live `.env` / `secrets.json` / FIX store in tree | `ABSENT` (good) |
| Product FileStore implementation | `MISSING` (ignore is pre-work) |

**A103-01 (P2):** add §6 delta before the first live QuickFIX Logon or the first `dotnet user-secrets set`.  
**A103-02 (P2):** mirror log/store/secret ignores into `mt5-sdk/.gitignore` because the parent gitlink cannot see them.  
**A103-03 (P3):** optional `.envrc` + `docker-compose.override.yml` when Compose lands.

No P0. Nothing to rotate. Product source untouched.
