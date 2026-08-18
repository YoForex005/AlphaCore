# R001 — env hunt (`MT5_PASSWORD`)

| Field | Value |
|---|---|
| Agent | R001 |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T08:23:31Z |
| Host | `DESKTOP-FQPFPKE` / user `ADMIN` |
| Artifact | `D:\Prop\reports\swarm\20260818\R001_env_hunt.md` |
| Assigned | Search `D:\Prop` for `.env`, `.env.local`, `appsettings` with a real `MT5_PASSWORD` (not the literal `<SECRET>`). List **paths** and **PLACEHOLDER / PRESENT (length only)**. Do not copy secret values. Do not modify product source. |
| Product source modified | **No.** This report is the only write. |
| Config / `.env*` / `appsettings` / user-secrets edited | **No.** |
| Secret values printed | **None.** Classification + length only. |
| Method | Recursive `Get-ChildItem -Force` under `D:\Prop` for `.env*`, `.env.local`, `appsettings*.json`. Classify `MT5_PASSWORD` by exact equality to `<SECRET>` (and known sentinels `replace_with_*`, `<ANGLE>`). Discard values after length. SHA-256 of `.env` recorded; value never copied. |

This is a **read-only confirmation**. It does not rewrite `.env`, `appsettings*.json`, or any file under `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, or `D:\Prop\mt5-sdk`.

**Masking rule:** live passwords are never copied. The literal token `<SECRET>` may be named because it is the assigned sentinel, not an operator secret.

---

## 0. Verdict

| Question | Result |
|---|---|
| Any `.env` / `.env.local` / `appsettings` with **real** `MT5_PASSWORD` (not literal `<SECRET>`)? | **YES — one file.** |
| Path | `D:\Prop\.env` |
| `MT5_PASSWORD` class | **PRESENT** |
| Value length (chars) | **8** |
| Exact match to literal `<SECRET>`? | **No** |
| `.env.local` anywhere under `D:\Prop`? | **No files** |
| Any `appsettings*.json` with `MT5_PASSWORD` key? | **No** |

`E001_no_secrets.md` (`.env` SHA `56C81786…`, 3408 bytes, slot class PLACEHOLDER) is **stale**. Current `.env` is **3484 bytes**, SHA-256 `a4ef94b990ee389c7e7900b599a60ae10e0c16e96e4b5da612302759958982d7`, LastWriteTimeUtc `2026-08-18T08:22:24.1111072Z`.

---

## 1. Classification rules

| Class | Meaning |
|---|---|
| **PRESENT** | Key exists; value is non-empty and is **not** the literal `<SECRET>` and **not** a known sentinel (`replace_with_*`, `<ANGLE_TOKEN>`). Length only. |
| **PLACEHOLDER** | Key exists; value is the literal `<SECRET>`, or `replace_with_*`, or another `<ANGLE>` sentinel. Length is of that sentinel. |
| **EMPTY** | Key exists; value length 0. |
| **NO_KEY** | File exists; no `MT5_PASSWORD` assignment / JSON property. |
| **FILE_ABSENT** | Path does not exist on disk. |

---

## 2. `.env` / `.env.local`

| Path | File | `MT5_PASSWORD` | Length |
|---|---|---|---:|
| `D:\Prop\.env` | exists (3484 bytes, gitignored: `.gitignore:28:.env`, not tracked) | **PRESENT** | 8 |
| `D:\Prop\.env.local` | **FILE_ABSENT** | — | — |
| `D:\Prop\.env.example` | **FILE_ABSENT** (working tree; git: ` D .env.example` vs HEAD) | — | — |
| `D:\Prop\.env.development` | **FILE_ABSENT** | — | — |
| `D:\Prop\.env.production` | **FILE_ABSENT** | — | — |
| `D:\Prop\apps\api\.env` | **FILE_ABSENT** | — | — |
| `D:\Prop\apps\api\.env.local` | **FILE_ABSENT** | — | — |
| `D:\Prop\apps\mt5-worker\.env` | **FILE_ABSENT** | — | — |
| `D:\Prop\apps\mt5-worker\.env.local` | **FILE_ABSENT** | — | — |
| `D:\Prop\apps\fix-worker\.env` | **FILE_ABSENT** | — | — |
| `D:\Prop\apps\fix-worker\.env.local` | **FILE_ABSENT** | — | — |
| `D:\Prop\apps\web\.env` | **FILE_ABSENT** | — | — |
| `D:\Prop\apps\web\.env.local` | **FILE_ABSENT** | — | — |
| `D:\Prop\src\.env` | **FILE_ABSENT** | — | — |
| `D:\Prop\mt5-sdk\.env` | **FILE_ABSENT** | — | — |
| `D:\Prop\mt5-sdk\.env.local` | **FILE_ABSENT** | — | — |
| `D:\Prop\mt5-sdk\.env.example` | exists (4999 bytes, template) | **PLACEHOLDER** (`replace_with_*`) | 29 |

Recursive `.env*` on disk: only `D:\Prop\.env` and `D:\Prop\mt5-sdk\.env.example`. Zero `.env.local` files.

---

## 3. `appsettings*.json`

No `MT5_PASSWORD` key (and no JSON `Password` / `Mt5*` property) in any of these files.

| Path | File | `MT5_PASSWORD` | Length |
|---|---|---|---:|
| `D:\Prop\apps\api\appsettings.json` | exists (1254 bytes) | **NO_KEY** | — |
| `D:\Prop\apps\api\appsettings.Development.json` | exists (478 bytes) | **NO_KEY** | — |
| `D:\Prop\apps\api\bin\Debug\net8.0\appsettings.json` | copy of source | **NO_KEY** | — |
| `D:\Prop\apps\api\bin\Debug\net8.0\appsettings.Development.json` | copy of source | **NO_KEY** | — |
| `D:\Prop\apps\mt5-worker\appsettings.json` | exists (137 bytes; logging only) | **NO_KEY** | — |
| `D:\Prop\apps\mt5-worker\appsettings.Development.json` | exists (137 bytes; logging only) | **NO_KEY** | — |
| `D:\Prop\apps\mt5-worker\bin\Debug\net8.0\appsettings.json` | copy of source | **NO_KEY** | — |
| `D:\Prop\apps\mt5-worker\bin\Debug\net8.0\appsettings.Development.json` | copy of source | **NO_KEY** | — |
| `D:\Prop\apps\mt5-worker\bin\Release\net8.0\appsettings.json` | copy of source | **NO_KEY** | — |
| `D:\Prop\apps\mt5-worker\bin\Release\net8.0\appsettings.Development.json` | copy of source | **NO_KEY** | — |
| `D:\Prop\apps\fix-worker\appsettings.json` | exists (137 bytes; logging only) | **NO_KEY** | — |
| `D:\Prop\apps\fix-worker\appsettings.Development.json` | exists (137 bytes; logging only) | **NO_KEY** | — |
| `D:\Prop\apps\fix-worker\bin\Debug\net8.0\appsettings.json` | copy of source | **NO_KEY** | — |
| `D:\Prop\apps\fix-worker\bin\Debug\net8.0\appsettings.Development.json` | copy of source | **NO_KEY** | — |

API `ConnectionStrings:Postgres` has a `Password=` keyword with an **empty** slot. That is **not** `MT5_PASSWORD` and is not counted as PRESENT.

---

## 4. Adjacent password keys in `D:\Prop\.env` (same file; values not copied)

Not assigned, but measured in the same parse so E001 is not silently trusted:

| Key | Class | Length |
|---|---|---:|
| `MT5_PASSWORD` | **PRESENT** | 8 |
| `MT5_STARWAVEFX_PASSWORD` | **PRESENT** | 11 |
| `ACHIEVER_PROXY_PASSWORD` | **PRESENT** | 15 |
| `CTRADER_FIX_PASSWORD` | **PLACEHOLDER** (literal `<SECRET>`) | 8 |
| `MT5_PASSWORD_ENCRYPTION_KEY` | **PLACEHOLDER** (`<ANGLE>` sentinel) | 27 |

---

## 5. Out of assigned file types (existence only)

| Location | Result |
|---|---|
| `%APPDATA%\Microsoft\UserSecrets\...\secrets.json` for Mt5Worker / FixWorker UserSecretsId | **FILE_ABSENT** |
| Process / User / Machine env name `MT5_PASSWORD` | **not inspected this pass** (file hunt only) |

---

## 6. Honesty

- **One live-looking `MT5_PASSWORD` slot is on disk:** gitignored `D:\Prop\.env`, class **PRESENT**, length **8**.
- Product `appsettings` do **not** carry `MT5_PASSWORD`.
- No `.env.local` exists.
- This report does **not** prove a live Manager session; it only classifies the file slot.
- No secret value is written here.
