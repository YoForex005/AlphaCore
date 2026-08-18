# E001 — No process secrets; `.env` existence is **not** “none”

| Field | Value |
|---|---|
| Agent | E001 (secrets / env presence only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T08:16:48Z (2026-08-18T13:46:48+05:30) |
| Host | `DESKTOP-FQPFPKE` / user `ADMIN` |
| Artifact | `D:\Prop\reports\swarm\20260818\E001_no_secrets.md` |
| Assigned | Confirm `D:\Prop` has no `.env` and this process has no `MT5_PASSWORD` / `CTRADER_FIX_PASSWORD` (names only). Write this file. Honest: live MT5 and live FIX cannot be proven. Do not print any secret values. Do not modify product source. |
| Product source modified | **No.** This report is the only write. |
| Config / `.env*` / `appsettings` / user-secrets edited | **No.** |
| Secret values printed | **None.** Classification + lengths only. |
| Method | Recursive `Get-ChildItem -Force` for `.env` / `.env.*`. `Test-Path Env:` + `[Environment]::GetEnvironmentVariable` for Process / User / Machine. Parse `.env` assignments; discard values after classifying. `git check-ignore` / `git ls-files` / `git status --porcelain`. File SHA-256. User-secrets root existence. Appsettings **name** scan only. |

This is a **read-only confirmation**. It does not rewrite `.env`, `.env.example`, `.gitignore`, `appsettings*.json`, or any file under `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, or `D:\Prop\mt5-sdk`.

**Masking rule:** live passwords, tokens, API keys, and credentialed URI userinfo are never copied. Known placeholder tokens (`<SECRET>`, `<BASE64_ENCODED_256BIT_KEY>`, other `<ANGLE>` tokens, `replace_with_*`) are classified by token class, not reprinted as if they were operator secrets. Non-secret flags (`false` / `true` / `Development` / `local`) may be quoted.

---

## 0. Verdict (binding — do not greenwash)

| Assigned claim | Measured result |
|---|---|
| “`D:\Prop` has no `.env`” | **FALSE.** `D:\Prop\.env` **exists** (3408 bytes, gitignored). |
| “This process has no `MT5_PASSWORD`” | **TRUE.** Name absent from Process, User, and Machine. |
| “This process has no `CTRADER_FIX_PASSWORD`” | **TRUE.** Name absent from Process, User, and Machine. |
| Live Achiever / StarwaveFX Manager session | **NOT PROVEN** (not attempted; no live password in process env). |
| Live cTrader FIX Logon (`35=A`) | **NOT PROVEN** (not attempted; no live password in process env). |

**Honest one-liner:** the process that ran this check does **not** carry `MT5_PASSWORD` or `CTRADER_FIX_PASSWORD`. A **gitignored** `D:\Prop\.env` **does** exist; its password **slots** are placeholder tokens, not operator secrets. Presence of a template `.env` plus absence of those two process names is **not** a live venue proof.

Do **not** treat this file as “repo is secret-free, therefore Achiever is connected.” Do **not** treat `CTRADER_FIX_ENABLED=true` in the ignored template as Logon. Do **not** treat `CREDENTIALS_AND_COPY_STATUS.md` / `C43` “no `.env`” as current — those are **stale**.

---

## 1. Assigned claim 1 — “no `.env` under `D:\Prop`”

**Rejected.** Recursive file search (`-Force`, so hidden names are visible):

| Path | Present | Bytes | LastWriteTimeUtc | SHA-256 |
|---|---|---:|---|---|
| `D:\Prop\.env` | **YES** | 3408 | 2026-08-18T07:36:45.8585461Z | `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA` |
| `D:\Prop\.env.example` | **NO** (working tree) | — | — | HEAD still tracks it; porcelain ` D .env.example` |
| `D:\Prop\mt5-sdk\.env` | **NO** | — | — | — |
| `D:\Prop\mt5-sdk\.env.example` | YES (template, not live) | 4999 | 2026-08-18T07:02:57.2656065Z | `937F7CB0A6912A05BEE0E5B672C696D6D4B41F63FFD530D2451C56715020C47C` |
| `D:\Prop\src\.env` | **NO** | — | — | — |
| `D:\Prop\apps\api\.env` | **NO** | — | — | — |
| `D:\Prop\apps\mt5-worker\.env` | **NO** | — | — | — |
| `D:\Prop\apps\fix-worker\.env` | **NO** | — | — | — |
| `D:\Prop\apps\web\.env` | **NO** | — | — | — |

A path `D:\Prop\apps\web\node_modules\axios\lib\env` exists and is a **vendor directory named `env`**, not a dotenv file. It is irrelevant to this check.

### Git status of the dotenv pair

| Check | Result |
|---|---|
| `D:\Prop\.gitignore` L28–L30 | `.env` / `.env.*` ignored; `!.env.example` un-ignored |
| `git check-ignore -v -- .env` | **ignored** (`.gitignore:28:.env`), exit 0 |
| `git ls-files -- .env` | **not tracked** |
| `git ls-files -- .env.example` | **tracked** |
| `git status --porcelain` for the pair | ` D .env.example` (deleted on disk vs HEAD) |

Consequence: the operator-looking file sits at a **gitignored** path. A clone does not receive `D:\Prop\.env`. That is a process/layout fact, not proof that the tree has “no `.env`.”

Same SHA-256 as D40 (`56C81786…`). D61 already measured this ignored file as the old `.env.example` text (password slots = tokens). This pass re-classified slots independently and agrees.

---

## 2. Assigned claim 2 — process has no `MT5_PASSWORD` / `CTRADER_FIX_PASSWORD`

**Confirmed for the measurement process** (PowerShell PID recorded at measure time; later PIDs in the same session also empty). **Names only — values were never read because the names are absent.**

| Name | Process | User | Machine | `Test-Path Env:<name>` | Empty-string present? |
|---|---|---|---|---|---|
| `MT5_PASSWORD` | **absent** | **absent** | **absent** | False | False |
| `CTRADER_FIX_PASSWORD` | **absent** | **absent** | **absent** | False | False |
| `MT5_LOGIN` (adjacent, not assigned) | absent | absent | absent | — | — |
| `MT5_SERVER` (adjacent, not assigned) | absent | absent | absent | — | — |
| `FIX_PASSWORD` (adjacent, not assigned) | absent | absent | absent | — | — |
| `CTRADER_PASSWORD` (adjacent, not assigned) | absent | absent | absent | — | — |

Process / User / Machine name scan for `PASSWORD`, `SECRET`, `CTRADER_FIX`, `MT5_`:

| Scope | Matching **names** (values not printed) |
|---|---|
| Process | `NEW_EX5_OPEN_MODE_MT5_LAUNCH` only — **not** a password name |
| User | `NEW_EX5_OPEN_MODE_MT5_LAUNCH` only |
| Machine | **none** |

`.NET` user-secrets: `%APPDATA%\Microsoft\UserSecrets` **does not exist**.

**What this does not prove:** another process on this machine (a worker started by a human, a scheduled task, a different shell that `dotenv`’d the file) was **not** inspected. This report speaks for **this** check process and the User/Machine persistent stores.

---

## 3. `D:\Prop\.env` slot classes (values discarded)

Parse: 115 lines, 26 comments, **77** assignments, 77 unique keys.

Password / key **slots** (the assigned names plus siblings):

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

`DATABASE_URL` shape (no value printed): **no URI scheme**, `Password=` present, password slot class `PLACEHOLDER_SECRET`, host-looking `localhost`/`127.0.0.1` token present. That is a Npgsql-style keyword string with a **placeholder** password, not a live credential.

`REDIS_URL` shape (no value printed): length 14, **no** `://user:pass@`, **no** `Password=`. Not a credentialed Redis URI.

Class census of all 77 keys (non-secret identifiers such as hosts, ports, group paths, and booleans fall into `NON_EMPTY_NOT_KNOWN_PLACEHOLDER` — that class is **not** “live secret”):

| Class | Count |
|---|---:|
| `PLACEHOLDER_SECRET` | 5 |
| `PLACEHOLDER_BASE64_KEY` | 1 |
| `PLACEHOLDER_ANGLE_TOKEN` | 4 (FIX SubID slots) |
| `EMPTY` | 2 (`ACHIEVER_PROXY_HOST`, `ACHIEVER_PROXY_PORT`) |
| `NON_EMPTY_NOT_KNOWN_PLACEHOLDER` | 65 (flags, hosts, ports, group names, logins, URLs without live password slots) |

`mt5-sdk\.env.example` (not live): has key `MT5_PASSWORD` as `replace_with_*` instructional placeholder; **no** `CTRADER_FIX_PASSWORD` key.

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

## 5. Appsettings name scan (no values printed)

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

## 6. Honesty pin — live MT5 and live FIX **cannot** be proven from this check

### 6.1 Why absence of two env names is not a live-PASS, and not a live-FAIL-of-connectivity-code either

A missing `MT5_PASSWORD` / `CTRADER_FIX_PASSWORD` in **this** process means:

- this process **cannot** have authenticated to Achiever Manager, StarwaveFX Manager, or cServer using those names;
- nobody can claim “we just logged on from this shell.”

It does **not** mean:

- the C# product has a live connector (C42: only `FakeMt5BrokerConnector`; G01 still FAIL);
- a diagnostic FIX Logon (`35=A`) was sent or answered (C43: **NOT PROVEN**; no `LOGON_OK` record);
- QuickFIX/n is wired (C19 / C43: official packages not referenced);
- dashboard “connected” / `LoggedOn` is venue truth (those rows are seeder/worker forgeries — anti-evidence).

### 6.2 Why a present `.env` is not a live-PASS either

The ignored file contains **placeholder** password slots (`PLACEHOLDER_SECRET`, length 8). Identifiers (hosts, numeric logins, FIX account id, SenderCompID, egress IP) may be present as **catalog / architecture** values — those are not passwords and are not reprinted here. Identifiers in a file **do not** open a socket.

No Manager TCP attach, no `mt5_group_probe`, no TLS to `live-us-eqx-01.p.c-trader.com:5211/:5212`, no pcap, no QuickFIX `FileStore`, no `LOGON_OK` artifact was produced by this agent.

### 6.3 Binding go-live implication

| Gate | This pass |
|---|---|
| A100 G01 live Achiever / StarwaveFX | **Still unproven** |
| A101 item 1 TRADE FIX Logon stable | **Still unproven** |
| Safe to set `REAL_COPY_EXECUTION_ENABLED=true` | **No** |
| Safe to treat venue as connected | **No** |

Siblings that remain the honesty pins: `C42_honesty_no_live_mt5.md`, `C43_honesty_no_live_fix.md`. This file does not reopen those code reviews; it only re-measures **env names + `.env` presence**.

---

## 7. Stale reports (do not copy their “no `.env`” sentence)

| Report | Stale sentence | Current measure |
|---|---|---|
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | `D:\Prop\.env` = **No** | **Yes** (3408 B, ignored, placeholders) |
| `C43_honesty_no_live_fix.md` § later body | “There is **no** `D:\Prop\.env`” | File now exists; C43’s **Logon NOT PROVEN** verdict is still correct |
| `A19` / early B-wave | “no `D:\Prop\.env`” in some tables | Superseded by D40 / D61 / this file |

D40 / D61 remain consistent with this SHA-256 and placeholder classification. Process-env absence of the two assigned names was **not** the D40 focus; it is the new measured fact here.

---

## 8. What was not done

- Did not print, log, or copy any secret **value**.
- Did not `set` / persist `MT5_PASSWORD` or `CTRADER_FIX_PASSWORD`.
- Did not load `.env` into the process.
- Did not connect to MT5 Manager, HTTP bridge, or cTrader FIX.
- Did not enable real copy.
- Did not edit product source.
- Did not restore `D:\Prop\.env.example` (working-tree delete predates this agent).
- Did not inspect other users’ processes or running `mt5-worker` / `fix-worker` address spaces.

---

## 9. Reproduction (names only)

```powershell
# .env presence (paths/sizes only)
Get-ChildItem -LiteralPath D:\Prop -Force -Recurse -File -ErrorAction SilentlyContinue |
  Where-Object { $_.Name -eq '.env' -or $_.Name -like '.env.*' } |
  Select-Object FullName, Length

# process / user / machine name presence — do not print values
foreach ($n in @('MT5_PASSWORD','CTRADER_FIX_PASSWORD')) {
  $p = [Environment]::GetEnvironmentVariable($n, 'Process')
  $u = [Environment]::GetEnvironmentVariable($n, 'User')
  $m = [Environment]::GetEnvironmentVariable($n, 'Machine')
  "{0} PROCESS={1} USER={2} MACHINE={3}" -f $n, ($null -ne $p), ($null -ne $u), ($null -ne $m)
}
```

Expected on this host at measure time: one `D:\Prop\.env`; both names `PROCESS=False USER=False MACHINE=False`.

---

## 10. Sign-off

| Item | Result |
|---|---|
| `D:\Prop\.env` absent? | **NO — file exists (gitignored, placeholder password slots)** |
| Process `MT5_PASSWORD`? | **NO** |
| Process `CTRADER_FIX_PASSWORD`? | **NO** |
| User / Machine those two names? | **NO** |
| User-secrets store? | **NO** |
| Live MT5 proven? | **NO** |
| Live FIX proven? | **NO** |
| Product source touched? | **NO** |
| Secret values in this report? | **NO** |
