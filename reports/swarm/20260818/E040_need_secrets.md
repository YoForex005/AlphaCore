# E040 — User must provide secrets locally in `.env` to test live logon

| Field | Value |
|---|---|
| Agent | E040 (need-secrets / live-logon blocker) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T08:21:55Z (2026-08-18T13:51:55+05:30) |
| Host | `DESKTOP-FQPFPKE` / user `ADMIN` / PID `57920` |
| Artifact | `D:\Prop\reports\swarm\20260818\E040_need_secrets.md` |
| Assigned | User must provide secrets locally in `.env` to test live logon. Write this file. Do not modify product source. |
| Product source modified | **No.** This report is the only write. |
| Config / `.env*` / `appsettings` / user-secrets edited | **No.** |
| Secret values printed | **None.** Classification + lengths only. |
| Binding law | Architecture §55–§56 (secrets stay out of Git; placeholders only in the example); §26 / A25 §3.6 (live Logon proof bar); A100 G01; A101 item 1 |
| Siblings (do not treat as this file) | E001 (process names + `.env` presence), D40 (product secret scan), D61 (example not placeholder-only), E002 / E016 (send-off / copy OFF), C42 (no live MT5), C43 (no live FIX Logon), `reports/CREDENTIALS_AND_COPY_STATUS.md` (stale “no `.env`”) |
| Method | `Test-Path` / `Get-Item -Force` / SHA-256 of `.env*`. `git check-ignore` / `git ls-files` / `git status --porcelain` for the dotenv pair. Process / User / Machine name presence via `[Environment]::GetEnvironmentVariable` (values discarded). Parse `.env` assignments; classify password slots; print flags only. Appsettings **name** scan. **No product edit. No Logon attempt. No secret reprint.** |

This is a **read-only confirmation**. It does not rewrite `.env`, `.env.example`, `.gitignore`, `appsettings*.json`, or any file under `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, or `D:\Prop\mt5-sdk`.

**Masking rule:** live passwords, tokens, API keys, and credentialed URI userinfo are never copied. Known placeholder tokens (`<SECRET>`, `<BASE64_ENCODED_256BIT_KEY>`, `<BROKER_ISSUED_VALUE>`, `replace_with_*`) are classified by token class. Non-secret flags (`false` / `true` / `Development` / `local`) may be quoted.

---

## 0. Verdict (binding — do not greenwash)

**Live Achiever / StarwaveFX / cTrader FIX logon cannot be tested on this machine until the operator fills real secrets in the local, gitignored `.env`.**

| Assigned claim | Measured result |
|---|---|
| Operator must supply secrets locally in `.env` before a live logon test | **TRUE.** Password slots are still `<SECRET>` (length 8). Process / User / Machine have **no** `MT5_PASSWORD` / `MT5_STARWAVEFX_PASSWORD` / `CTRADER_FIX_PASSWORD`. User-secrets store **absent**. |
| A filled `.env` already exists | **FALSE.** `D:\Prop\.env` exists (3408 B, gitignored) but is the old example blob — placeholder password slots, not operator secrets. |
| This process can authenticate to Achiever Manager, StarwaveFX Manager, or cServer | **NO.** No live password in any inspected store. |
| Filling `.env` **alone** proves Logon | **NO.** Necessary, **not sufficient**. C42: only `FakeMt5BrokerConnector`. C43: no QuickFIX initiator, no TLS, no `35=A` record. |
| Safe to treat venue as connected | **No** |
| Safe to set `REAL_COPY_EXECUTION_ENABLED=true` | **No** |

**Honest one-liner:**

```text
NEED_SECRETS = YES
D:\Prop\.env EXISTS but password slots = <SECRET>
process/user/machine MT5_PASSWORD / CTRADER_FIX_PASSWORD = ABSENT
live logon = NOT TESTABLE and NOT PROVEN
```

Do **not** invent passwords. Do **not** commit a filled `.env`. Do **not** treat `CTRADER_FIX_ENABLED=true` in the ignored template as Logon. Do **not** treat `CREDENTIALS_AND_COPY_STATUS.md` “`D:\Prop\.env` = No” as current — the file exists; it is still unfilled.

---

## 1. Why live logon is blocked (need-secrets)

A live logon test needs a **non-placeholder** password in a **local, untracked** store. Measured stores:

| Store | Present? | Venue password usable for Logon? |
|---|---|---|
| `D:\Prop\.env` | **Yes** (gitignored) | **No** — `PLACEHOLDER_SECRET` (`<SECRET>`, len 8) on all venue password keys |
| `D:\Prop\.env.example` (working tree) | **No** (` D .env.example`) | N/A |
| HEAD `.env.example` | Tracked; same bytes as ignored `.env` | **No** — same tokens |
| Process env `MT5_PASSWORD` | **Absent** | **No** |
| Process env `MT5_STARWAVEFX_PASSWORD` | **Absent** | **No** |
| Process env `CTRADER_FIX_PASSWORD` | **Absent** | **No** |
| User / Machine those names | **Absent** | **No** |
| `%APPDATA%\Microsoft\UserSecrets` | **Does not exist** | **No** |
| Host `appsettings*.json` | Exist | **No** `Password` JSON key; no `MT5_PASSWORD` / `CTRADER_FIX_PASSWORD` names |
| `mt5-sdk\.env` | **No** | **No** |

Hosts and logins in the ignored template are **non-secret identifiers** (architecture §56 catalog). Identifiers do not open a socket. Passwords were **never supplied**.

Therefore:

1. **This agent will not attempt** Achiever / StarwaveFX Manager attach or cTrader FIX `35=A`.
2. **No later agent should claim** a live logon PASS from this tree until the operator pastes issued secrets into **local** `.env` (or user-secrets) **and** a diagnostic Logon record exists (A25 §3.6).
3. **This agent will not invent** those secrets.

---

## 2. `D:\Prop\.env` — present, unfilled, gitignored

| Path | Present | Bytes | LastWriteTimeUtc | SHA-256 |
|---|---|---:|---|---|
| `D:\Prop\.env` | **YES** | 3408 | 2026-08-18T07:36:45.8585461Z | `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA` |
| `D:\Prop\.env.example` | **NO** (working tree) | — | — | HEAD still tracks it; porcelain ` D .env.example` |
| `D:\Prop\mt5-sdk\.env` | **NO** | — | — | — |
| `D:\Prop\mt5-sdk\.env.example` | YES (SDK template) | 4999 | 2026-08-18T07:02:57.2656065Z | `937F7CB0A6912A05BEE0E5B672C696D6D4B41F63FFD530D2451C56715020C47C` |
| `D:\Prop\src\.env` | **NO** | — | — | — |
| `D:\Prop\apps\api\.env` | **NO** | — | — | — |
| `D:\Prop\apps\mt5-worker\.env` | **NO** | — | — | — |
| `D:\Prop\apps\fix-worker\.env` | **NO** | — | — | — |
| `D:\Prop\apps\web\.env` | **NO** | — | — | — |

Same SHA-256 as E001 / D40 / D61. D61 already proved this ignored file is **byte-identical** to HEAD `.env.example` (git blob `b71480a8…`). It is **not** an operator fill.

### Git status of the dotenv pair

| Check | Result |
|---|---|
| `D:\Prop\.gitignore` L28–L30 | `.env` / `.env.*` ignored; `!.env.example` un-ignored |
| `git check-ignore -v -- .env` | **ignored** (`.gitignore:28:.env`), exit 0 |
| `git ls-files -- .env` | **not tracked** |
| `git ls-files -- .env.example` | **tracked** |
| `git status --porcelain` for the pair | ` D .env.example` |

Consequence: a clone does **not** receive `D:\Prop\.env`. The operator sheet belongs at that ignored path. Filling it later stays local **if** nobody runs `git add -f .env`.

---

## 3. Password slots still placeholders (values discarded)

Parse of `D:\Prop\.env`: 115 lines, 26 comments, 12 blank, **77** assignments.

Venue / key slots (names + class + length only):

| Key | Present | Class | Raw value length |
|---|---|---|---:|
| `MT5_PASSWORD` | Yes | `PLACEHOLDER_SECRET` (`<SECRET>`) | 8 |
| `MT5_STARWAVEFX_PASSWORD` | Yes | `PLACEHOLDER_SECRET` | 8 |
| `CTRADER_FIX_PASSWORD` | Yes | `PLACEHOLDER_SECRET` | 8 |
| `ACHIEVER_PROXY_PASSWORD` | Yes | `PLACEHOLDER_SECRET` | 8 |
| `ACHIEVER_PROXY_USERNAME` | Yes | `PLACEHOLDER_SECRET` | 8 |
| `MT5_PASSWORD_ENCRYPTION_KEY` | Yes | `PLACEHOLDER_BASE64_KEY` | 27 |
| `DATABASE_URL` password slot (`Password=`) | Yes | `PLACEHOLDER_SECRET` (slot length 8) | URI/string length 83 |
| `REDIS_URL` password slot | No | `NO_PASSWORD_SLOT` | string length 14 |

`DATABASE_URL` shape (no value printed): **no** URI scheme, `Password=` present, password slot class `PLACEHOLDER_SECRET`, `localhost` token present. Npgsql-style keyword string with a **placeholder** password.

`REDIS_URL` shape: length 14, **no** `://user:pass@`, **no** `Password=`. Not a credentialed Redis URI.

Class census of all 77 keys:

| Class | Count |
|---|---:|
| `PLACEHOLDER_SECRET` | 5 |
| `PLACEHOLDER_BASE64_KEY` | 1 |
| `PLACEHOLDER_ANGLE_TOKEN` | 4 (FIX SubID slots) |
| `EMPTY` | 2 (`ACHIEVER_PROXY_HOST`, `ACHIEVER_PROXY_PORT`) |
| `NON_EMPTY_NOT_KNOWN_PLACEHOLDER` | 65 (flags, hosts, ports, group names, logins, URLs without live password slots) |

**Need-secrets implication:** the three logon passwords (`MT5_PASSWORD`, `MT5_STARWAVEFX_PASSWORD`, `CTRADER_FIX_PASSWORD`) are **tokens**. Until those three slots (and, if a tunnel is used, the proxy pair) are replaced with **issued** values in **local** `.env`, a live logon test is **blocked**.

`mt5-sdk\.env.example` (not live): `MT5_PASSWORD=replace_with_*`; **no** `CTRADER_FIX_PASSWORD` key; **no** `mt5-sdk\.env` on disk.

---

## 4. Process / User / Machine — names absent

**Names only.** Values were never read because the names are absent.

| Name | Process | User | Machine | `Test-Path Env:<name>` |
|---|---|---|---|---|
| `MT5_PASSWORD` | **absent** | **absent** | **absent** | False |
| `MT5_STARWAVEFX_PASSWORD` | **absent** | **absent** | **absent** | False |
| `CTRADER_FIX_PASSWORD` | **absent** | **absent** | **absent** | False |
| `ACHIEVER_PROXY_PASSWORD` | **absent** | **absent** | **absent** | False |
| `ACHIEVER_PROXY_USERNAME` | **absent** | **absent** | **absent** | False |
| `MT5_PASSWORD_ENCRYPTION_KEY` | **absent** | **absent** | **absent** | False |
| `MT5_LOGIN` / `MT5_SERVER` (adjacent) | absent | absent | absent | False |
| `FIX_PASSWORD` / `CTRADER_PASSWORD` (adjacent) | absent | absent | absent | False |
| `DATABASE_URL` | absent | absent | absent | False |

Name scan for `PASSWORD` / `SECRET` / `CTRADER_FIX` / `MT5_` / `API_KEY` / `TOKEN`:

| Scope | Matching **names** (values not printed) |
|---|---|
| Process | `NEW_EX5_OPEN_MODE_MT5_LAUNCH` only — **not** a password name |
| User | `NEW_EX5_OPEN_MODE_MT5_LAUNCH` only |
| Machine | **none** |

`.NET` user-secrets: `%APPDATA%\Microsoft\UserSecrets` **does not exist**.

**What this does not prove:** another process on this machine that `dotenv`’d a private file was **not** inspected. This report speaks for **this** check process and the User/Machine persistent stores.

---

## 5. Safe-to-print flags from the ignored `.env` (not secrets)

| Key | Value |
|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | `false` |
| `FEATURE_COPY_TRADING_ENABLED` | `false` |
| `CTRADER_FIX_ENABLED` | `true` |
| `CTRADER_FIX_TRADE_SESSION_ENABLED` | `true` |
| `ACHIEVER_PROXY_ENABLED` | `false` |
| `ASPNETCORE_ENVIRONMENT` | `Development` |
| `MT5_MODE` | `local` |

`CTRADER_FIX_ENABLED=true` in a **template** this process did **not** load is **not** a TLS Logon. `REAL_COPY_EXECUTION_ENABLED=false` is the correct send-off floor (E002 / E016). Send-off is **orthogonal** to “Logon proven.”

---

## 6. Appsettings name scan (no values printed)

No `secrets.json` under `D:\Prop`. No `appsettings.Production.json` / `Staging` in this pass.

| File | Mentions `MT5_PASSWORD` | Mentions `CTRADER_FIX_PASSWORD` | JSON `"Password":` key | SHA-256 |
|---|---|---|---|---|
| `apps\api\appsettings.json` | No | No | No | `69D41CAD…5984AD20` |
| `apps\api\appsettings.Development.json` | No | No | No | `81B5E6DC…E8B0481` |
| `apps\mt5-worker\appsettings.json` | No | No | No | `AB16B7B7…A6FF33` |
| `apps\mt5-worker\appsettings.Development.json` | No | No | No | same |
| `apps\fix-worker\appsettings.json` | No | No | No | same |
| `apps\fix-worker\appsettings.Development.json` | No | No | No | same |

Venue secrets do **not** live in committed JSON. That is correct §55 hygiene. It is also why **local `.env` (or user-secrets) is the only legitimate place** for the operator to put them.

---

## 7. What the operator must put in local `.env` (when they choose to test logon)

**Do not paste issued values into this report, into Git, into `appsettings.json`, or into any `VITE_*` key.** Copy the ignored file from the example, then replace **only** the secret slots from the **current** broker-issued forms.

Minimum slots for a live logon **test** (names only):

| Key | Required for | Current class | Operator action |
|---|---|---|---|
| `MT5_PASSWORD` | Achiever Manager logon | `PLACEHOLDER_SECRET` | Replace `<SECRET>` with issued Manager password |
| `MT5_STARWAVEFX_PASSWORD` | StarwaveFX Manager logon | `PLACEHOLDER_SECRET` | Replace `<SECRET>` |
| `CTRADER_FIX_PASSWORD` | cTrader FIX Logon tag 554 | `PLACEHOLDER_SECRET` | Replace `<SECRET>` |
| `CTRADER_FIX_*_SENDER_SUB_ID` / `*_TARGET_SUB_ID` | FIX headers (§26) | `PLACEHOLDER_ANGLE_TOKEN` | Replace `<BROKER_ISSUED_VALUE>` from the **current** form + RoE |
| `ACHIEVER_PROXY_USERNAME` / `ACHIEVER_PROXY_PASSWORD` | Only if proxy is enabled | `PLACEHOLDER_SECRET` | Leave tokens if `ACHIEVER_PROXY_ENABLED=false` |

Keep:

- `.env` gitignored (already).
- `REAL_COPY_EXECUTION_ENABLED=false` until §68 / §70 pass (A100 / A101).
- Hosts / logins as the operator’s own sheet. Do **not** commit a filled file.

Filling these slots is **authorization material**, not a connector. After fill, a later increment still needs:

- a real MT5 Manager / HTTP-bridge path (C42: today only `FakeMt5BrokerConnector`);
- a real FIX initiator + TLS to `:5211` / `:5212` (C43: QuickFIX/n **not referenced**);
- A25 §3.6 `LOGON_OK` records for **both** QUOTE and TRADE.

Until those exist, even a filled `.env` cannot produce a measured live logon.

---

## 8. Honesty pin — filling secrets later does not auto-PASS go-live

| Gate | This pass |
|---|---|
| Operator secrets available for a live logon test | **NO — NEED_SECRETS** |
| A100 G01 live Achiever / StarwaveFX | **Still unproven** (C42) |
| A101 item 1 TRADE FIX Logon stable | **Still unproven** (C43) |
| Live `35=D` / copy send | **OFF** / **SAFE_BY_ABSENCE** (E002 / E016) |
| Safe to treat venue as connected | **No** |
| Safe to enable real copy | **No** |

Siblings that remain the honesty pins: `E001_no_secrets.md` (presence + process names), `C42_honesty_no_live_mt5.md`, `C43_honesty_no_live_fix.md`. This file adds the **operator action**: secrets must be supplied locally in `.env` before anyone may claim a live logon **test** was even possible.

---

## 9. Stale reports

| Report | Stale sentence | Current measure |
|---|---|---|
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | `D:\Prop\.env` = **No** | **Yes** (3408 B, ignored, placeholders) — “passwords never supplied” is still true |
| `C43` body “no `D:\Prop\.env`” | File missing | File now exists; C43 **Logon NOT PROVEN** is still correct |
| Early A19 / B-wave “no `.env`” | — | Superseded by D40 / D61 / E001 / this file |

---

## 10. What was not done

- Did not print, log, or copy any secret **value**.
- Did not invent or persist `MT5_PASSWORD` / `MT5_STARWAVEFX_PASSWORD` / `CTRADER_FIX_PASSWORD`.
- Did not load `.env` into the process.
- Did not connect to MT5 Manager, HTTP bridge, or cTrader FIX.
- Did not send `35=A` or `35=D`.
- Did not enable real copy.
- Did not edit product source.
- Did not restore `D:\Prop\.env.example`.
- Did not inspect other users’ processes or running worker address spaces.

---

## 11. Reproduction (names only)

```powershell
# .env presence (paths/sizes only)
Get-ChildItem -LiteralPath D:\Prop -Force -Recurse -File -ErrorAction SilentlyContinue |
  Where-Object { $_.Name -eq '.env' -or $_.Name -like '.env.*' } |
  Select-Object FullName, Length

# process / user / machine name presence — do not print values
foreach ($n in @('MT5_PASSWORD','MT5_STARWAVEFX_PASSWORD','CTRADER_FIX_PASSWORD')) {
  $p = [Environment]::GetEnvironmentVariable($n, 'Process')
  $u = [Environment]::GetEnvironmentVariable($n, 'User')
  $m = [Environment]::GetEnvironmentVariable($n, 'Machine')
  "{0} PROCESS={1} USER={2} MACHINE={3}" -f $n, ($null -ne $p), ($null -ne $u), ($null -ne $m)
}
```

Expected on this host at measure time: one `D:\Prop\.env`; all three names `PROCESS=False USER=False MACHINE=False`.

To classify slots without printing secrets: treat value `<SECRET>` (len 8) as `PLACEHOLDER_SECRET`. A live fill has a different class and a length that is **not** 8-plus-that-token.

---

## 12. Sign-off

| Item | Result |
|---|---|
| User must provide secrets locally in `.env` to test live logon? | **YES** |
| `D:\Prop\.env` exists? | **YES** (gitignored, example bytes) |
| Venue password slots filled? | **NO** (`PLACEHOLDER_SECRET`) |
| Process / User / Machine logon passwords? | **NO** |
| User-secrets store? | **NO** |
| Live MT5 logon testable / proven? | **NO** |
| Live FIX logon testable / proven? | **NO** |
| Product source touched? | **NO** |
| Secret values in this report? | **NO** |

**Product source was not modified. This file is the only output.**
