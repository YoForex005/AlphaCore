# D62 — Root `.gitignore` recensus (measured)

| Field | Value |
|---|---|
| Agent | D62 (repo hygiene / ignore recensus) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:40:18+05:30 |
| Artifact | `D:\Prop\reports\swarm\20260818\D62_gitignore.md` |
| Workspace assigned | `D:\Prop\src` (file is **not** under `src/`) |
| Product tree | Repo-root `D:\Prop\.gitignore` |
| Assigned | Read `.gitignore`. Write this report. **Do not modify product source.** |
| Product source modified | **No.** This report (plus `INDEX.md` / `SWARM_LOG.md` catalog notes) is the only write. |
| Method | Full read of root + `mt5-sdk` ignore files; SHA-256 + git blob; BOM / EOL / line count; recursive hunt for other ignore files; `git check-ignore -v --no-index` on 80+ paths; `git status --ignored`; classify on-disk `.env` keys **without** dumping values; compare worktree to HEAD `398a142` and to A103 / B40. |
| Law | Architecture v2 §55 (secrets never in Git; placeholders only in `.env.example`); A103 recommended ignore delta (not applied); A35 FileStore / A50–A76 FileLog ban; A75 env-example law. |
| Relates | A19 / B25 secrets, A75 env example, A103 ignore rec, B40 env+ignore confirm, A65 / C12 / D63 compose, A35 / A50 / A76 FIX store+log, D59 `_tmp_*` scratch. |
| HEAD at measure | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`, 2026-08-18 13:24:21 +0530) |
| `.gitignore` first commit | `6c414477f632416031b851171d3354fe2a232594` (2026-08-18 13:12:17 +0530, Initial commit) |
| Worktree vs HEAD (`.gitignore`) | **Clean.** `git hash-object .gitignore` = `HEAD:.gitignore` = `f4c0070786c9de4b57a9a86e79b88d43a76a6f18` |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

This is a **gitignore recensus**. It is **not** a rewrite of the file, **not** a claim that A103 §6 was applied, and **not** a go-live PASS.

---

## 0. Verdict

**Root `.gitignore` exists, is tracked, and is byte-identical to HEAD. A103 §6 was never applied. The file is the right §55 shape for `.env` / `.env.example`, but it does not cover the paths the dirty worktree now names.**

| Question | Result |
|---|---|
| Does `D:\Prop\.gitignore` exist? | **YES** — 73 physical lines, 1107 bytes, LF, no BOM |
| SHA-256 | `FAE817C1C2F9AD9BEA4353D89A82ED015585A449FC1339561F2C966A0C2B21E0` — **unchanged vs A103 / B40** |
| `src/.gitignore`? | **No.** Only two ignore files in the tree: root + `mt5-sdk/.gitignore` |
| `.env` / `.env.*` / `!.env.example`? | **YES** (lines 28–30). Measured: `.env` ignored; `.env.example` un-ignored. |
| A103 §6 delta applied? | **No.** Same blob as the A103 snapshot. |
| Live passwords in Git? | **No** (reconfirmed). `.env` on disk is placeholders only and is ignored. |
| Is ignore policy finished? | **No.** Treat as **EXISTS_NEEDS_REFACTOR**. |

Worktree facts that **post-date** A103 / B40 and change the urgency:

1. `D:\Prop\.env.example` is **deleted** from the worktree (` D .env.example`). The same 3408-byte blob now lives as ignored `D:\Prop\.env` (git hash-object `b71480a8…` = `HEAD:.env.example`). Secret slots are still `<SECRET>` / `<BROKER_ISSUED_VALUE>` / `<BASE64_ENCODED_256BIT_KEY>`. `REAL_COPY_EXECUTION_ENABLED=false`. **Do not commit the deletion** — that would drop the §55 placeholder catalog from Git.
2. Dirty `apps/api/appsettings.json` now sets `FileStorePath: "./fixstore"` and `FileLogPath: "./fixlogs"`. Both directory names are **OPEN** (`git check-ignore` prints nothing). `CTraderFixOptions` still has **no** `FileStorePath` / `FileLogPath` properties, so the keys are unread today — but they are the names a later QuickFIX wiring will use.
3. Untracked `docker-compose.yml` exists (SHA `1ED8787F…`) with `POSTGRES_PASSWORD: ti_dev_only`. `docker-compose.override.yml` is **OPEN**. `.dockerignore` is **MISSING**.

§73.B for the **root ignore file as a whole:** `EXISTS_NEEDS_REFACTOR`.  
§73.B for `.env` / `.env.*` / `!.env.example`: `EXISTS_AND_GOOD`.  
§73.B for `fixstore/` + `fixlogs/` (the paths the dirty API config now names): `MISSING`.  
§73.B for A35 `log/quote|trade` + QuickFIX `*.body` / `*.header`: `MISSING` (same as A103).  
§73.B for `.dockerignore`: `MISSING`.

No P0. Nothing to rotate. Product source untouched.

---

## 1. What exists on disk

### 1.1 Ignore files

| Path | Present? | Bytes | SHA-256 | Git |
|---|---|---:|---|---|
| `D:\Prop\.gitignore` | **Yes** | 1107 | `FAE817C1C2F9AD9BEA4353D89A82ED015585A449FC1339561F2C966A0C2B21E0` | tracked, clean vs HEAD (`100644 f4c0070786c9de4b57a9a86e79b88d43a76a6f18`) |
| `D:\Prop\mt5-sdk\.gitignore` | **Yes** | 482 | `06D08A304754CE6801C2413C1C05373DA90AAD6803C2F0D16E4BAA4028A67F87` | nested repo (parent gitlink `160000 a8f3fe85bc0adf109acb5ec72ed8adb2c0a289df`) |
| `D:\Prop\src\.gitignore` | **No** | — | — | — |
| `D:\Prop\apps/**/.gitignore` | **No** | — | — | — |
| `D:\Prop\tests\.gitignore` | **No** | — | — | — |
| `D:\Prop\reports\.gitignore` | **No** | — | — | — |
| `D:\Prop\.dockerignore` | **No** | — | — | — |
| `D:\Prop\.gitattributes` | **No** | — | — | — |

`Get-ChildItem -Force -Recurse -Filter .gitignore` returned **exactly two** files. Parent `git ls-files -- '*gitignore*'` returns only `.gitignore`.

`mt5-sdk` is a **separate git repo** recorded in the parent as a gitlink. There is **no** `.gitmodules`. Parent ignore rules **do not** apply inside `mt5-sdk\`.

### 1.2 Root file metadata

| Property | Value |
|---|---|
| Full path | `D:\Prop\.gitignore` |
| Length | 1107 bytes |
| Physical lines | 73 (trailing newline present) |
| Encoding | UTF-8, **no BOM** |
| EOL | **LF only** (`HasCRLF=False`) |
| Last write | 2026-08-18T13:08:02.2614146+05:30 |
| Active rules (non-comment, non-blank) | **38** |
| Sections | 8 comment banners (`.NET`, `Secrets & Environment`, `FIX session state`, `Node / React`, `Python`, `Logs`, `OS`, `Vendor SDK`) |

### 1.3 Adjacent secret / store / log files (not ignore bugs unless noted)

| Path | On disk? | Git |
|---|---|---|
| `D:\Prop\.env` | **Yes** (3408 B, SHA-256 `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA`) | **ignored** (`!! .env`). Byte-identical to `HEAD:.env.example` |
| `D:\Prop\.env.example` | **No** | **tracked, deleted in worktree** (` D .env.example`) |
| `D:\Prop\mt5-sdk\.env` | No | would be ignored by nested repo |
| `D:\Prop\mt5-sdk\.env.example` | Yes (4999 B) | tracked in nested repo |
| Any `secrets.json` / `*.pfx` / `*.pem` / `*.key` / `*.p12` | **No** | `secrets.json` + `*.pfx` would be ignored at parent |
| `store/`, `fix-store/`, `fixstore/`, `fixlogs/`, `log/`, `logs/` | **No** | first two ignored; `fixstore/` + `fixlogs/` + `log/` **open** |
| `build-release.log`, `test-release.log` | Yes | ignored (`*.log`) |
| `apps/*/appsettings.Development.json` | Yes | ignored, untracked |
| `apps/*/appsettings.json` | Yes | **tracked**. API file is **dirty** vs HEAD |
| `docker-compose.yml` | Yes (687 B) | **untracked** |
| `docker-compose.override.yml` | No | would be **open** |

`git rev-list --all -- .env` is empty. A live `.env` has **never** been committed.

---

## 2. Complete root `.gitignore` (HEAD = worktree)

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

`# *.dll` is a comment, not a rule. Parent `git ls-files -- '*.dll'` = **0** (vendor DLLs live inside the `mt5-sdk` gitlink). Do **not** add a blanket `*.dll` that would fight a future in-tree `FIX44-cTrader.xml` companion or a deliberate SDK copy.

---

## 3. Rule-by-rule census (38 active rules)

### 3.1 `.NET` (17 rules)

| Rule | Line | Measured | Class |
|---|---:|---|---|
| `bin/` | 4 | `src/*/bin/`, `apps/*/bin/`, `tests/*/bin/`, `reports/swarm/20260818/_tmp_*/bin/` all `!!` | `EXISTS_AND_GOOD` |
| `obj/` | 5 | same for `obj/` | `EXISTS_AND_GOOD` |
| `out/` | 6 | no such dir today | `EXISTS_AND_GOOD` |
| `build/` | 7 | no parent `build/` (CMake `build/` is inside the gitlink) | `EXISTS_AND_GOOD` |
| `.vs/` | 8 | ignored | `EXISTS_AND_GOOD` |
| `.idea/` | 9 | ignored | `EXISTS_AND_GOOD` |
| `*.user` | 10 | also matches `*.DotSettings.user` and `Directory.Build.props.user` | `EXISTS_AND_GOOD` |
| `*.suo` | 11 | ignored | `EXISTS_AND_GOOD` |
| `*.userosscache` | 12 | ignored | `EXISTS_AND_GOOD` |
| `*.sln.docstates` | 13 | ignored | `EXISTS_AND_GOOD` |
| `[Dd]ebug/` | 14 | any folder named Debug/debug | standard VS; aggressive but conventional |
| `[Rr]elease/` | 15 | any folder named Release/release | same |
| `artifacts/` | 16 | ignored | `EXISTS_AND_GOOD` |
| `TestResults/` | 17 | ignored | `EXISTS_AND_GOOD` |
| `coverage/` | 18 | also covers `apps/web/coverage/` | `EXISTS_AND_GOOD` |
| `*.pdb` | 19 | ignored | `EXISTS_AND_GOOD` |
| `*.log` | 20 | `build-release.log`, `test-release.log`, `propfirm_backend.log` | `EXISTS_AND_GOOD` |

Open next to this section: `*.clef`, `*.nupkg`, `packages.lock.json`, `.vscode/`, `*.coverage`. None exist as secret stores.

### 3.2 Secrets & environment (7 rules)

| Rule | Line | Measured | Class |
|---|---:|---|---|
| `.env` | 28 | `D:\Prop\.env` → `!!`; `apps/web/.env` ignored | `EXISTS_AND_GOOD` |
| `.env.*` | 29 | `.env.local`, `.env.production`, `apps/web/.env.development.local` ignored | `EXISTS_AND_GOOD` |
| `!.env.example` | 30 | `.env.example` **not** ignored (correct). File is missing from worktree — restore before commit | `EXISTS_AND_GOOD` (rule) / worktree **regression** (file) |
| `*.pfx` | 31 | `certs/client.pfx` ignored | `EXISTS_AND_GOOD` (narrow) |
| `secrets.json` | 32 | basename anywhere, including `user-secrets/secrets.json` | `EXISTS_AND_GOOD` (narrow) |
| `appsettings.Development.json` | 33 | all three host Development files `!!` | keep (A103 §8.1) |
| `appsettings.*.local.json` | 34 | `appsettings.Staging.local.json` ignored | `EXISTS_AND_GOOD` |

Open: `.envrc`, `*.pem`, `*.key`, `*.p12`, `*.snk`, `.user-secrets/fix.json` (non-canonical dump).

`appsettings.json` (non-Development) and `appsettings.Production.json` are **OPEN** and **must stay that way** — operators must not paste passwords there. Worktree API `appsettings.json` currently has empty `Password=` / empty `EmergencyFlattenApiKey`. Still the file people will paste a live FIX password into.

### 3.3 FIX session state (4 rules)

| Rule | Line | Covers | Gap |
|---|---:|---|---|
| `store/` | 39 | `store/quote`, `apps/fix-worker/store/…` | **Over-broad** — also matches `src/Infrastructure/Persistence/Store/` and `apps/web/src/store/` (measured `IGN`). No such product folder today. |
| `fix-store/` | 40 | hyphenated name only | does **not** match `fixstore/` |
| `*.seqnums` | 41 | CWD fallback FileStore | good |
| `*.session` | 42 | CWD fallback FileStore | good |

`CTraderFixOptions` (worktree) has host / CompIDs / password / `RealCopyExecutionEnabled` only. **No** `FileStorePath` / `FileLogPath` properties. Dirty API JSON nevertheless names:

```json
"FileStorePath": "./fixstore",
"FileLogPath": "./fixlogs"
```

Measured:

| Path | Ignored? |
|---|---|
| `store/quote` | yes (`store/`) |
| `fix-store/quote` | yes (`fix-store/`) |
| `fixstore/quote` | **NO** |
| `fixlogs/quote` | **NO** |
| `apps/api/fixstore/x` | **NO** |
| `apps/api/fixlogs/x` | **NO** |
| `apps/fix-worker/FIX.4.4-x.seqnums` | yes |
| `apps/fix-worker/FIX.4.4-x.session` | yes |
| `apps/fix-worker/FIX.4.4-x.body` | **NO** |
| `apps/fix-worker/FIX.4.4-x.header` | **NO** |
| `log/quote` / `log/trade` | **NO** |

A50 / A76: product session factory must **reject** `FileLogPath`. Ignore is the last backstop. FileLog can contain tag `554=<password>`. The dirty JSON already writes the forbidden key name.

### 3.4 Node / React (3 rules)

`node_modules/`, `dist/`, `apps/web/.vite/` — all measured ignored. `apps/web/node_modules/` is `!!`. `package-lock.json` is untracked and **not** ignored (correct to commit later).

### 3.5 Python (3 rules)

`services/ml-service/.venv/`, `services/ml-service/__pycache__/`, `*.pyc`. `D:\Prop\services\ml-service` **does not exist** (C44 / B39: ML not built). Root `.venv/` / `venv/` / generic `__pycache__/` are **OPEN**.

### 3.6 Logs / OS / vendor

`logs/` is plural only. Singular `log/` and `fix-log/` are **OPEN**. OS junk (`Thumbs.db`, `Desktop.ini`, `.DS_Store`) works. Vendor banner is a comment; it does not un-ignore anything.

`reports/` is **not** ignored. Correct — swarm artifacts are the permanent store. `_tmp_*/bin` and `_tmp_*/obj` are ignored via `bin/` / `obj/` (D59: those trees are scratch, not product).

---

## 4. `check-ignore` evidence (parent repo)

Ignored (keep):

```text
.env
.env.local
.env.production
apps/web/.env
apps/web/.env.development.local
secrets.json
apps/fix-worker/secrets.json
user-secrets/secrets.json
apps/api/appsettings.Development.json
apps/api/appsettings.Staging.local.json
store/quote
fix-store/quote
apps/fix-worker/FIX.4.4-x.seqnums
apps/fix-worker/FIX.4.4-x.session
logs/app.log
build-release.log
test-release.log
propfirm_backend.log
bin/  obj/  node_modules/  dist/  apps/web/.vite/
certs/client.pfx
```

Open (still the A103 hole list, plus the new worktree names):

```text
.env.example          # un-ignored on purpose; FILE MISSING in worktree
.envrc
.user-secrets/fix.json
apps/api/appsettings.json
apps/api/appsettings.Production.json
fixstore/quote
fixlogs/quote
apps/api/fixstore/x
apps/api/fixlogs/x
apps/fix-worker/FIX.4.4-x.body
apps/fix-worker/FIX.4.4-x.header
log/quote
log/trade
fix-log/quote
log-20260818.txt
app.clef
certs/client.pem
certs/client.key
certs/client.p12
certs/signing.snk
docker-compose.yml
docker-compose.override.yml
.vscode/settings.json
*.nupkg
```

`git status --ignored --porcelain` shows **43** `!!` lines: the live `.env`, three Development JSON files, every product `bin/`+`obj/`, `apps/web/node_modules/`, two root `*.log` files, and eight `_tmp_*/bin|obj` scratch trees. **No** ignored FIX store or `logs/` directory exists yet.

Tracked build output: `bin/**` = 0, `obj/**` = 0, `*.dll` = 0, `*.pdb` = 0, `node_modules/**` = 0.

---

## 5. Nested `mt5-sdk/.gitignore`

Complete nested file (482 bytes):

```gitignore
# Secrets — only .env.example is tracked
.env
.env.*
!.env.example

# Build output
build/
build-*/
out/
cmake-build-*/
CMakeCache.txt
CMakeFiles/
CMakeUserPresets.json
compile_commands.json

# MSVC / Windows
*.obj
*.pdb
*.ilk
*.exp
*.idb
*.tlog
*.vcxproj.user
.vs/

# Compiled artefacts (vendor/MetaTrader5SDK/Libs is tracked deliberately)
*.o
*.a
*.so
*.dylib
*.exe
!vendor/MetaTrader5SDK/**

# Editors / OS
.idea/
*.swp
.DS_Store
Thumbs.db
```

Measured inside that repo (`git -C D:\Prop\mt5-sdk check-ignore -v --no-index`):

| Path | Nested result |
|---|---|
| `.env` | ignored (line 2) |
| `.env.example` | **not** ignored (line 4) |
| `.env.local` | ignored (`.env.*`) |
| `secrets.json` | **OPEN** |
| `foo.log` / `logs/x` / `propfirm_backend.log` | **OPEN** |
| `store/x` | **OPEN** |
| `certs/a.pem` | **OPEN** |

A103-02 is still open. Parent cannot close it.

---

## 6. `.env` worktree (ignore works; catalog file is gone)

| Check | Result |
|---|---|
| `git check-ignore -v -- .env` | `.gitignore:28:.env` |
| `git status --ignored --porcelain -- .env .env.example` | ` D .env.example` + `!! .env` |
| WT `.env` git-hash-object | `b71480a8d9f0cd30166c25e1d124ab744a08fa2f` |
| `HEAD:.env.example` | **same blob** |
| Secret slots | `MT5_PASSWORD`, proxy user/pass, `MT5_STARWAVEFX_PASSWORD`, `CTRADER_FIX_PASSWORD`, FIX SubIDs, `DATABASE_URL` `Password=`, `MT5_PASSWORD_ENCRYPTION_KEY` — all **placeholder** |
| `DATABASE_URL` host | `localhost` |
| `REDIS_URL` | `localhost:6379` (no password slot — A75 catalog gap, not a leak) |
| `REAL_COPY_EXECUTION_ENABLED` | `false` |
| Filled live password? | **None** |

Ignore policy did its job: the filled-in local file is ignored. The **regression** is that the tracked example was removed from the worktree. A later `git add -u` would delete `.env.example` from `main`.

Live venue **identifiers** (hosts, manager logins, account `1369850`, SenderCompID) are in that blob — same A19 / A75 / B40 P2. That is content policy, not an ignore miss. D62 does not rewrite the example.

User-secrets: workers still have `UserSecretsId`s; API still has none. `%APPDATA%\Microsoft\UserSecrets` was not re-probed this pass (B40: directory missing). The in-repo `secrets.json` rule still covers a copied dump with the canonical name.

---

## 7. What must stay tracked

| Path | Why |
|---|---|
| `.gitignore` | this policy |
| `.env.example` | §55 placeholder catalog — **restore the worktree copy before any commit** |
| `mt5-sdk/.env.example` | SDK template (nested repo) |
| `apps/*/appsettings.json` | non-secret host config. Keep passwords empty / move to env |
| `apps/*/Properties/launchSettings.json` | localhost profiles |
| `*.csproj` `UserSecretsId` | identifier only |
| `reports/` / `docs/` | lab record |
| `docker-compose.yml` (when committed) | local-dev topology (D63). Ignore the **override**, not the file |
| `mt5-sdk/vendor/MetaTrader5SDK/Libs/*.dll` | tracked deliberately in the nested repo |

Do **not** ignore `reports/`, `docs/`, or a future `FIX44-cTrader.xml`.

---

## 8. Delta vs A103 / B40 (honesty)

| Claim | Still true? |
|---|---|
| Root `.gitignore` SHA `FAE817C1…` | **Yes** |
| A103 §6 additions on disk | **No** — still a recommendation |
| B40 “`.env.example` disk = HEAD” | **Stale.** File is deleted; bytes moved to ignored `.env` |
| B40 “no real `.env` on disk” | **Stale.** `.env` exists; it is the example blob, ignored |
| B40 “HEAD `appsettings.json` is Logging only” | **HEAD still is.** Worktree API file is **not** |
| A103 “no `docker-compose.yml`” | **Stale.** File is untracked on disk (D63) |
| Four A103 holes (`log/`, `*.body`/`*.header`, TLS extras, compose override) | **Still open** |
| New hole | `fixstore/` + `fixlogs/` match dirty API JSON |

A103’s “80% done” still holds for the **file**. The worktree around it is dirtier.

---

## 9. Recommended next ignore edit (do **not** apply in this task)

A later I0 increment should merge A103 §6 **and** these two names the dirty API config already uses:

```gitignore
fixstore/
fixlogs/
file-store/
log/
fix-log/
*.body
*.header
.envrc
docker-compose.override.yml
*.p12
*.pem
*.key
*.snk
.user-secrets/
user-secrets/
*.clef
log-*.txt
```

Narrow `store/` if a product `Store/` or React `src/store/` folder is added (today it would be silently untracked). Prefer ` /store/` (repo-root only) plus `fix-store/` / `fixstore/`, or require FileStore under those names only.

Append A103 §7 to `mt5-sdk/.gitignore` in the **nested** repo.

Add a `.dockerignore` when compose is first committed (A65 §5.4): at least `.env`, `**/bin`, `**/obj`, `node_modules`, `reports`, `mt5-sdk/vendor`.

Confirm after the edit:

```powershell
git -C D:\Prop check-ignore -v --no-index -- .env .env.example secrets.json store/quote fixstore/quote fixlogs/quote log/quote apps/fix-worker/FIX.4.4-x.body
git -C D:\Prop ls-files -- .env secrets.json store fix-store fixstore fixlogs logs
```

Expected: `.env.example` is the only listed path that is **not** ignored; `ls-files` prints only `.env.example` among those names.

Never `git add -f` `.env`, `secrets.json`, `store/`, `fixstore/`, `fixlogs/`, `log/`, or `*.pfx`.

---

## 10. Findings (report-only; no product edits)

| ID | Sev | Finding |
|---|---|---|
| D62-01 | — | **PASS on the assigned file.** Root `.gitignore` exists, is tracked, SHA `FAE817C1…`, clean vs HEAD `398a142`. `.env` / `.env.*` / `!.env.example` work. |
| D62-02 | P2 | A103 §6 still unapplied. Dirty `apps/api/appsettings.json` now names `./fixstore` and `./fixlogs`; both are **OPEN**. FileLog is forbidden (A50/A76) and can hold tag 554. |
| D62-03 | P2 | Worktree deleted `.env.example` and left an identical ignored `.env`. Restore the example before commit. Do not `git add -u` hygiene paths blindly. |
| D62-04 | P2 | `mt5-sdk/.gitignore` still has no `*.log` / `logs/` / `secrets.json` / `store/` / TLS globs. Parent gitlink cannot see them. |
| D62-05 | P3 | Same leftover holes: `docker-compose.override.yml`, `.envrc`, `log/`, QuickFIX `*.body`/`*.header`, `*.pem`/`*.key`/`*.p12`, `.dockerignore`. |
| D62-06 | P3 | `store/` is over-broad (`src/**/Store/`, `apps/web/src/store/` would be ignored). No collision today. |
| D62-07 | P3 | Untracked `docker-compose.yml` embeds `POSTGRES_PASSWORD=ti_dev_only` (D63). Ignore the override, not the compose file; do not reuse that password in prod. |

No I0 rewrite performed. This agent did not edit `.gitignore`, `.env`, `.env.example`, `appsettings.json`, compose, or any product C#.

---

## 11. Classification summary

| Item | Class |
|---|---|
| Root `.gitignore` exists and is tracked | `EXISTS_AND_GOOD` (file present) / `EXISTS_NEEDS_REFACTOR` (policy incomplete) |
| `.env` / `.env.*` / `!.env.example` | `EXISTS_AND_GOOD` |
| Worktree `.env.example` | `MISSING` (deleted; HEAD still has it) |
| `secrets.json` + `*.pfx` | `EXISTS_AND_GOOD` (narrow) |
| `store/` + `fix-store/` + `*.seqnums` + `*.session` | `EXISTS_AND_GOOD` (hyphen name) |
| `fixstore/` + `fixlogs/` (dirty API paths) | `MISSING` |
| `log/` + QuickFIX `*.body` / `*.header` | `MISSING` |
| `logs/` + `*.log` | `EXISTS_AND_GOOD` |
| TLS extras + user-secrets folder aliases | `MISSING` |
| `.dockerignore` | `MISSING` |
| `mt5-sdk` log/store/secret ignores | `MISSING` (nested repo) |
| Live password committed | `ABSENT` (good) |
| Product FileStore implementation | `MISSING` (ignore is pre-work; options type has no path properties) |

**Answer:** the root `.gitignore` is real, tracked, and correct for env-file shape. It is **not** complete, and the worktree has made two of the remaining holes concrete (`fixstore`/`fixlogs`, deleted `.env.example`). Do not treat ignore policy as done. Do not modify product source in this pass.
