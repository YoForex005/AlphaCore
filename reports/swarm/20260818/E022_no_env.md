# E022 — Confirm **no** `.env`: **REJECTED** at repo root

| Field | Value |
|---|---|
| Agent | E022 (`.env` presence only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T08:20:45Z (2026-08-18T13:50:45+05:30) |
| Host | `DESKTOP-FQPFPKE` / user `ADMIN` / PID `43284` (presence pass) |
| Artifact | `D:\Prop\reports\swarm\20260818\E022_no_env.md` |
| Assigned | Confirm no `.env`. Write this file. Do not print secrets. Do not modify product source. |
| Product source modified | **No.** This report is the only product-adjacent write besides the swarm log entry. |
| Config / `.env*` / `appsettings` / user-secrets edited | **No.** |
| Secret values printed | **None.** Classification + lengths only. |
| Workspace | `D:\Prop\src` (no `.env` there). Scope of the claim is the `D:\Prop` tree. |
| Method | `Test-Path -LiteralPath` on canonical paths. Recursive `Get-ChildItem -Force` for `.env` / `.env.*` / `.envrc`. SHA-256 via `Get-FileHash`. `git check-ignore -v`, `git ls-files`, `git hash-object`, `git status --porcelain --ignored`, `git rev-list --all -- .env`. Parse assignments; **discard values** after classifying. Process / User / Machine name presence only (`[Environment]::GetEnvironmentVariable`). Appsettings **name** scan only. |

This is a **read-only confirmation**. It does not rewrite `.env`, `.env.example`, `.gitignore`, `appsettings*.json`, or any file under `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, or `D:\Prop\mt5-sdk`.

**Masking rule:** live passwords, tokens, API keys, and credentialed URI userinfo are never copied. Known placeholder tokens (`<SECRET>`, `<BASE64_ENCODED_256BIT_KEY>`, `<BROKER_ISSUED_VALUE>`, other `<ANGLE>` tokens, `replace_with_*`) are classified by token class, not reprinted as operator secrets. Non-secret flags (`false` / `true` / `Development` / `local`) may be quoted.

---

## 0. Verdict (binding — do not greenwash)

| Assigned claim | Measured result |
|---|---|
| “There is no `.env`” (repo `D:\Prop`) | **FALSE.** `D:\Prop\.env` **exists** (3408 bytes, gitignored, not tracked). |
| “There is no `.env` under workspace `D:\Prop\src`” | **TRUE.** |
| “There is no `.env` under `apps/`, `tests/`, `docs/`, `scripts/`, `services/`, `reports/`, `mt5-sdk/`” | **TRUE.** |
| “Git history never contained a live `.env`” | **TRUE.** `git rev-list --all -- .env` is empty. |
| Live Achiever / StarwaveFX Manager session | **NOT PROVEN** (not attempted). |
| Live cTrader FIX Logon (`35=A`) | **NOT PROVEN** (not attempted). |

**Honest one-liner:** the assigned sentence “no `.env`” is **false** for the repo root. The file is the old tracked `.env.example` bytes sitting on the **ignored** name. Product source trees and this process do **not** carry a filled operator sheet. Presence of a gitignored template `.env` is **not** a live venue proof.

Do **not** treat this file as “repo is secret-free, therefore Achiever is connected.” Do **not** treat `CTRADER_FIX_ENABLED=true` in the ignored template as Logon. Do **not** copy `CREDENTIALS_AND_COPY_STATUS.md` / early C43 “no `.env`” as current — those are **stale**.

---

## 1. Assigned claim — “no `.env` under `D:\Prop`”

**Rejected.** Recursive file search (`-Force`, so hidden names are visible):

| Path | Present | Bytes | LastWriteTimeUtc | SHA-256 |
|---|---|---:|---|---|
| `D:\Prop\.env` | **YES** | 3408 | 2026-08-18T07:36:45.8585461Z | `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA` |
| `D:\Prop\.env.example` | **NO** (working tree) | — | — | HEAD still tracks it; porcelain ` D .env.example` |
| `D:\Prop\.env.local` / `.env.development` / `.env.production` / `.envrc` | **NO** | — | — | — |
| `D:\Prop\src\.env` | **NO** | — | — | — |
| `D:\Prop\apps\api\.env` | **NO** | — | — | — |
| `D:\Prop\apps\mt5-worker\.env` | **NO** | — | — | — |
| `D:\Prop\apps\fix-worker\.env` | **NO** | — | — | — |
| `D:\Prop\apps\web\.env` (+ Vite `.env.local` / `.env.development` / `.env.production`) | **NO** | — | — | — |
| `D:\Prop\tests\.env` | **NO** | — | — | — |
| `D:\Prop\docs\.env` / `scripts\.env` / `services\.env` / `reports\.env` | **NO** | — | — | — |
| `D:\Prop\mt5-sdk\.env` | **NO** | — | — | — |
| `D:\Prop\mt5-sdk\.env.example` | YES (template, not live) | 4999 | 2026-08-18T07:02:57.2656065Z | `937F7CB0A6912A05BEE0E5B672C696D6D4B41F63FFD530D2451C56715020C47C` |

Recursive count of files named `.env` / `.env.*` / `.envrc` under `D:\Prop` (including vendor): **2**. Those two rows are the only hits. `D:\Prop\apps\web\node_modules\axios\lib\env` is a **vendor directory named `env`**, not a dotenv file. It is irrelevant to this check.

Split that must not be collapsed:

| Scope | `.env` present? |
|---|---|
| Repo root `D:\Prop` | **Yes** (ignored) |
| Product C# / React / tests (`src`, `apps`, `tests`) | **No** |
| SDK live sheet `mt5-sdk\.env` | **No** |
| Git index / history | **No** |

---

## 2. Git status of the dotenv pair

| Check | Result |
|---|---|
| `D:\Prop\.gitignore` L28–L30 | `.env` / `.env.*` ignored; `!.env.example` un-ignored |
| `.gitignore` SHA-256 | `FAE817C1C2F9AD9BEA4353D89A82ED015585A449FC1339561F2C966A0C2B21E0` (1107 B) |
| `git check-ignore -v -- .env` | **ignored** (`.gitignore:28:.env`), exit 0 |
| `git ls-files -- .env` | **not tracked** (`--error-unmatch` exit 1) |
| `git ls-files -- .env.example` | **tracked** (`100644 blob b71480a8d9f0cd30166c25e1d124ab744a08fa2f`) |
| `git hash-object -- .env` | `b71480a8d9f0cd30166c25e1d124ab744a08fa2f` (**= HEAD `.env.example`**) |
| `git rev-list --all -- .env` | **empty** — name never existed in parent history |
| `git status --porcelain --ignored` for the pair | ` D .env.example` + `!! .env` |
| `HEAD` | `398a14200ec65714c4077eed55c46808382ca1e3` (`main`) |

Consequence: the operator-looking file sits at a **gitignored** path and is **byte-identical** to the committed example. A clone does not receive `D:\Prop\.env`. That is a process/layout fact, not proof that the tree has “no `.env`.”

Same SHA-256 as D40 / D61 / E001 (`56C81786…`). This pass re-hashed independently and agrees.

`.gitignore` shape (lines 28–30):

```gitignore
.env
.env.*
!.env.example
```

A restored `.env.example` would be tracked again; a real filled `.env` would stay ignored. Ignore shape is correct. The assigned **absence** claim is still false.

---

## 3. `D:\Prop\.env` slot classes (values discarded)

Parse: 115 lines, 26 comments, 12 blanks, **77** assignments, 77 unique keys.

Password / key **slots** (names + class + length only):

| Key | Present | Class | Raw value length |
|---|---|---|---:|
| `MT5_PASSWORD` | Yes | `PLACEHOLDER_SECRET` (`<SECRET>`, 8 chars) | 8 |
| `CTRADER_FIX_PASSWORD` | Yes | `PLACEHOLDER_SECRET` | 8 |
| `MT5_STARWAVEFX_PASSWORD` | Yes | `PLACEHOLDER_SECRET` | 8 |
| `ACHIEVER_PROXY_PASSWORD` | Yes | `PLACEHOLDER_SECRET` | 8 |
| `ACHIEVER_PROXY_USERNAME` | Yes | `PLACEHOLDER_SECRET` | 8 |
| `MT5_PASSWORD_ENCRYPTION_KEY` | Yes | `PLACEHOLDER_BASE64_KEY` | 27 |
| `DATABASE_URL` password slot (`Password=`) | Yes | `PLACEHOLDER_SECRET` (slot length 8) | URI/string length 83 |
| `REDIS_URL` password slot | No | `NO_PASSWORD_SLOT` | string length 14 |
| `REDIS_PASSWORD` | **No** | absent key | — |
| `POSTGRES_PASSWORD` | **No** | absent key | — |

`DATABASE_URL` shape (no value printed): **no URI scheme**, `Password=` present, password slot class `PLACEHOLDER_SECRET`, host-looking `localhost` / `127.0.0.1` token present. That is a Npgsql-style keyword string with a **placeholder** password, not a live credential.

`REDIS_URL` shape (no value printed): length 14, **no** `://user:pass@`, **no** `Password=`. Not a credentialed Redis URI.

Class census of all 77 keys (non-secret identifiers such as hosts, ports, group paths, and booleans fall into `NON_EMPTY_NOT_KNOWN_PLACEHOLDER` — that class is **not** “live secret”):

| Class | Count |
|---|---:|
| `PLACEHOLDER_SECRET` | 5 |
| `PLACEHOLDER_BASE64_KEY` | 1 |
| `PLACEHOLDER_ANGLE_TOKEN` | 4 (FIX SubID slots) |
| `EMPTY` | 2 (`ACHIEVER_PROXY_HOST`, `ACHIEVER_PROXY_PORT`) |
| `NON_EMPTY_NOT_KNOWN_PLACEHOLDER` | 65 (flags, hosts, ports, group names, logins, URLs without live password slots) |

`mt5-sdk\.env.example` (not live): instructional `replace_with_*` / empty slots. **No** `CTRADER_FIX_PASSWORD` key. **No** `mt5-sdk\.env`.

This file is **not** a filled operator sheet. It is the renamed example. Identifiers that may exist in the 65 non-placeholder rows are **not reprinted** here (catalog / architecture values, not passwords). Identifiers in a file **do not** open a socket.

---

## 4. Safe-to-print flags from the ignored `.env` (not secrets)

These are configuration **booleans / mode labels**, not credentials:

| Key | Value |
|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | `false` |
| `FEATURE_COPY_TRADING_ENABLED` | `false` |
| `CTRADER_FIX_ENABLED` | `true` |
| `CTRADER_FIX_TRADE_SESSION_ENABLED` | `true` |
| `ACHIEVER_PROXY_ENABLED` | `false` |
| `ASPNETCORE_ENVIRONMENT` | `Development` |
| `MT5_MODE` | `local` |

`CTRADER_FIX_ENABLED=true` in a **template** file that this process did **not** load is **not** a TLS Logon. `REAL_COPY_EXECUTION_ENABLED=false` is the correct send-off floor; send-off is **orthogonal** to “Logon proven.”

---

## 5. Process / User / Machine — assigned secret **names**

**Names only — values were never read because the names are absent.**

| Name | Process | User | Machine | `Test-Path Env:` |
|---|---|---|---|---|
| `MT5_PASSWORD` | **absent** | **absent** | **absent** | False |
| `CTRADER_FIX_PASSWORD` | **absent** | **absent** | **absent** | False |
| `MT5_STARWAVEFX_PASSWORD` | absent | absent | absent | False |
| `MT5_LOGIN` / `MT5_SERVER` | absent | absent | absent | False |
| `FIX_PASSWORD` / `CTRADER_PASSWORD` | absent | absent | absent | False |
| `DATABASE_URL` / `REDIS_PASSWORD` | absent | absent | absent | False |

Name scan for `PASSWORD`, `SECRET`, `CTRADER_FIX`, `MT5_`:

| Scope | Matching **names** (values not printed) |
|---|---|
| Process | `NEW_EX5_OPEN_MODE_MT5_LAUNCH` only — **not** a password name |
| User | `NEW_EX5_OPEN_MODE_MT5_LAUNCH` only |
| Machine | **none** |

`.NET` user-secrets: `%APPDATA%\Microsoft\UserSecrets` **does not exist**. `D:\Prop\secrets.json` **does not exist**.

**What this does not prove:** another process on this machine (a worker started by a human, a scheduled task, a different shell that `dotenv`’d the file) was **not** inspected. This report speaks for **this** check process and the User/Machine persistent stores.

---

## 6. Appsettings name scan (no values printed)

No `secrets.json` under `D:\Prop`. No `appsettings.Production.json` / `Staging` in this pass.

| File | Mentions `MT5_PASSWORD` | Mentions `CTRADER_FIX_PASSWORD` | JSON `"Password":` key |
|---|---|---|---|
| `apps\api\appsettings.json` | No | No | No |
| `apps\api\appsettings.Development.json` | No | No | No |
| `apps\mt5-worker\appsettings.json` | No | No | No |
| `apps\mt5-worker\appsettings.Development.json` | No | No | No |
| `apps\fix-worker\appsettings.json` | No | No | No |
| `apps\fix-worker\appsettings.Development.json` | No | No | No |

This scan does **not** re-audit connection-string interiors (D40 already documented an empty `Password=` on API Postgres). This agent did not dump those files.

---

## 7. Honesty pin — live MT5 and live FIX **cannot** be proven from this check

### 7.1 Why a present `.env` is not a live-PASS

The ignored file contains **placeholder** password slots (`PLACEHOLDER_SECRET`, length 8). This process did **not** load the file. No Manager TCP attach, no `mt5_group_probe`, no TLS to a cTrader FIX host, no pcap, no QuickFIX `FileStore`, no `LOGON_OK` artifact was produced by this agent.

### 7.2 Why “no `.env` under `src/`” is not a live-PASS either

Absence of dotenv files in product trees means workers are **not** reading a local sheet from those folders. It does **not** mean:

- the C# product has a live connector (C42: only `FakeMt5BrokerConnector`; G01 still FAIL);
- a diagnostic FIX Logon (`35=A`) was sent or answered (C43: **NOT PROVEN**);
- QuickFIX/n is wired (C19 / D52: official packages not referenced);
- dashboard “connected” / `LoggedOn` is venue truth (E008: seeder + worker persist `Disconnected`).

### 7.3 Binding go-live implication

| Gate | This pass |
|---|---|
| A100 G01 live Achiever / StarwaveFX | **Still unproven** |
| A101 item 1 TRADE FIX Logon stable | **Still unproven** |
| Safe to set `REAL_COPY_EXECUTION_ENABLED=true` | **No** |
| Safe to treat venue as connected | **No** |

Siblings: `E001_no_secrets.md` (same SHA; process names absent), `D61_env.md` (example is not placeholder-only for identifiers), `C42_honesty_no_live_mt5.md`, `C43_honesty_no_live_fix.md`. This file does not reopen those code reviews; it only re-measures **`.env` presence**.

---

## 8. Stale reports (do not copy their “no `.env`” sentence)

| Report | Stale sentence | Current measure |
|---|---|---|
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | `D:\Prop\.env` = **No** | **Yes** (3408 B, ignored, placeholders) |
| `C43_honesty_no_live_fix.md` (later body) | “There is **no** `D:\Prop\.env`” | File now exists; C43’s **Logon NOT PROVEN** verdict is still correct |
| `B40_gitignore_env.md` §6 | `D:\Prop\.env` = no | File now exists (rename of the example) |
| `A19` / early B-wave | “no `D:\Prop\.env`” in some tables | Superseded by D40 / D61 / E001 / this file |

D40 / D61 / E001 remain consistent with this SHA-256 and placeholder classification.

---

## 9. What was not done

- Did not print, log, or copy any secret **value**.
- Did not reprint live venue identifiers (hosts, logins, CompIDs, account ids, egress).
- Did not `set` / persist `MT5_PASSWORD` or `CTRADER_FIX_PASSWORD`.
- Did not load `.env` into the process.
- Did not connect to MT5 Manager, HTTP bridge, or cTrader FIX.
- Did not enable real copy.
- Did not edit product source.
- Did not restore `D:\Prop\.env.example` (working-tree delete predates this agent).
- Did not delete or rewrite `D:\Prop\.env`.
- Did not inspect other users’ processes or running `mt5-worker` / `fix-worker` address spaces.

---

## 10. Reproduction (paths / names only)

```powershell
# .env presence (paths/sizes only — do not Get-Content)
Get-ChildItem -LiteralPath D:\Prop -Force -Recurse -File -ErrorAction SilentlyContinue |
  Where-Object { $_.Name -eq '.env' -or $_.Name -like '.env.*' } |
  Select-Object FullName, Length

Test-Path -LiteralPath D:\Prop\.env
Test-Path -LiteralPath D:\Prop\src\.env

# git (from D:\Prop)
git check-ignore -v -- .env
git ls-files -- .env
git status --porcelain --ignored -- .env .env.example
```

Expected on this host at measure time: one `D:\Prop\.env` (3408 B); `D:\Prop\src\.env` absent; `check-ignore` exit 0; porcelain `!! .env` and ` D .env.example`.

---

## 11. Sign-off

| Item | Result |
|---|---|
| Assigned “no `.env`” (repo root) | **FAIL — `D:\Prop\.env` exists** |
| Workspace / product `src\.env` | **ABSENT** |
| Apps / tests / SDK live `.env` | **ABSENT** |
| File gitignored / untracked / never in history | **YES / YES / YES** |
| Password slots in that file | **placeholders only** (`<SECRET>` / key token) |
| Process `MT5_PASSWORD` / `CTRADER_FIX_PASSWORD` | **ABSENT** |
| User-secrets store | **NO** |
| Live MT5 proven? | **NO** |
| Live FIX proven? | **NO** |
| Product source touched? | **NO** |
| Secret values in this report? | **NO** |

**Answer to the assigned confirmation:** cannot confirm “no `.env`.” Confirm instead: **one gitignored root `.env` exists; it is the example blob; product trees have none; no live passwords printed or present in this process.**
