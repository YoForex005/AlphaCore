# D61 — `D:\Prop\.env.example`: placeholders only?

| Field | Value |
|---|---|
| Agent | D61 (env-example placeholder audit) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:40:56+05:30 |
| Artifact | `D:\Prop\reports\swarm\20260818\D61_env.md` |
| Assigned | Read `D:\Prop\.env.example`. Placeholders only? Write this file. Do not modify product source. |
| Product source edited | **No** |
| Config / `.env*` edited | **No** (working-tree delete of `.env.example` was already present; this agent did not restore or rewrite it) |
| Law | Architecture v2 §55 (“Create only placeholders in `.env.example`”) + §56 catalog; A75 placeholder convention |
| Prior (same SHA, not copied as verdict) | A19, A75, A103, B25, B40 |
| Method | `Test-Path` + `Get-ChildItem -Force` for `.env*`. `git ls-files` / `git show HEAD:.env.example` / `git hash-object` / `git diff --stat`. SHA-256 of ignored `D:\Prop\.env`. Full key=value parse of the HEAD blob (77 assignments). Read `mt5-sdk/.env.example`, `.gitignore` lines 28–30, `CTraderFixOptions.cs` Host/CompID defaults. Nothing answered from memory. |

This is a **read-only confirmation**. It does not rewrite `.env.example`, `.env`, `.gitignore`, `appsettings.json`, or any product C#.

---

## 0. Verdict

**No. The committed example is not placeholder-only.**

| Question | Measured answer |
|---|---|
| Does `D:\Prop\.env.example` exist **on disk** right now? | **NO** |
| Does Git `HEAD` still track `.env.example`? | **YES** — `100644 blob b71480a8d9f0cd30166c25e1d124ab744a08fa2f` |
| Working-tree status | `D .env.example` (115 lines deleted vs HEAD). Content now sits at **ignored** `D:\Prop\.env` |
| Is that ignored `.env` a filled operator sheet? | **NO** — byte-identical to HEAD `.env.example` (same SHA-256, same git blob) |
| Are **password / key** slots placeholders? | **YES** — six `<SECRET>`, four `<BROKER_ISSUED_VALUE>`, one `<BASE64_ENCODED_256BIT_KEY>` |
| Are **all** values placeholders? | **NO** — live manager IPs, logins, FIX host, FIX account `1369850`, SenderCompID, egress IP, `demo\`/`contest\` group paths |
| `REAL_COPY_EXECUTION_ENABLED` | **`false`** (correct safety floor) |
| §55 “no production **secrets** in Git”? | **PASS** (passwords are tokens) |
| §55 / A75 “placeholders **only** in `.env.example`”? | **FAIL** (live venue identifiers committed) |

**One-line:** Secret slots are tokens; targeting values are a copy of architecture §56. The assigned path is missing from the working tree because the tracked example was renamed onto ignored `.env` without changing a byte.

Do **not** treat “passwords are `<SECRET>`” as “the file is placeholder-only.” Do **not** treat the missing working-tree file as “the example was scrubbed” — `git checkout -- .env.example` would restore the same live identifiers.

---

## 1. What was actually on disk vs in Git

### 1.1 Assigned path

| | |
|---|---|
| Path | `D:\Prop\.env.example` |
| `Test-Path` | **False** |
| `git ls-files` | still listed (index = last commit) |
| `git diff HEAD -- .env.example` | `115 deletions` (entire file gone from worktree) |
| HEAD blob | `b71480a8d9f0cd30166c25e1d124ab744a08fa2f` |
| HEAD commit that added it | `6c414477f632416031b851171d3354fe2a232594` (2026-08-18 13:12:17 +0530, Initial commit) |
| Current `HEAD` | `398a14200ec65714c4077eed55c46808382ca1e3` (`main`) |

### 1.2 Working-tree stand-in (ignored)

| | |
|---|---|
| Path | `D:\Prop\.env` |
| Bytes | 3408 |
| Lines | 115 |
| LastWriteUtc | 2026-08-18 07:36:45 |
| SHA-256 | `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA` |
| `git hash-object` | `b71480a8d9f0cd30166c25e1d124ab744a08fa2f` (**= HEAD `.env.example`**) |
| `git check-ignore -v` | `.gitignore:28:.env` |
| Tracked? | **No** (`git ls-files --error-unmatch .env` fails) |

B40 recorded the **same** SHA-256 and blob for the then-on-disk `.env.example`. The file was not rewritten; it was **moved** onto the ignored name.

### 1.3 Other env files

| Path | On disk | Tracked | SHA-256 | Role |
|---|---|---|---|---|
| `D:\Prop\.env.example` | **absent** | yes (HEAD) | (blob above) | assigned subject |
| `D:\Prop\.env` | yes | no (ignored) | same as HEAD example | accidental rename, not a live fill |
| `D:\Prop\mt5-sdk\.env.example` | yes | nested gitlink | `937F7CB0A6912A05BEE0E5B672C696D6D4B41F63FFD530D2451C56715020C47C` | SDK-only; not the assigned file |
| `D:\Prop\mt5-sdk\.env` | **no** | — | — | — |
| Any other `.env*` under `D:\Prop` (excluding `node_modules`/`bin`/`obj`) | **no** | — | — | — |

`.gitignore` lines 28–30 remain:

```gitignore
.env
.env.*
!.env.example
```

So a restored `.env.example` would be tracked again; a real filled `.env` would stay ignored. That ignore shape is correct. The **content law** is the failure.

---

## 2. Binding law (what “placeholders only” means)

Architecture §55 (quoted):

> Create only placeholders in `.env.example`.

A75 §2 is the operational definition used here (stricter than §56’s printed listing):

| Token | Allowed as a committed value? |
|---|---|
| `<SECRET>` | yes — password / key slot |
| `<BROKER_ISSUED_VALUE>` | yes — CompID / SubID slot |
| `<MT5_MANAGER_HOST>`, `<MANAGER_LOGIN>`, `<EGRESS_IP>`, `<CTRADER_FIX_HOST>`, `<FIX_ACCOUNT_ID>`, `<PLAN_GROUP_PATH>` | yes — identity slots |
| Live IP, manager login, FIX account, `live.*.<account>` SenderCompID, live host, live egress, live `demo\`/`contest\` paths | **no** |
| Ports `443` / `5211` / `5212` / `5201` / `5202` / `5432` / `6379` | yes — public protocol defaults |
| `REAL_COPY_EXECUTION_ENABLED=false` | **required** |
| Session qualifier literals `QUOTE` / `TRADE` | yes — protocol names |
| `CTRADER_FIX_USE_SSL=true` | yes |

§56 **names** the keys. The §56 **block as printed in v2 is itself not placeholder-only** (live Achiever / StarwaveFX / Pepperstone identifiers, only passwords and SubIDs masked). This report follows the **stricter §55 / A75** rule, which is the assigned question.

---

## 3. Secret slots — PASS (placeholders)

77 `KEY=value` lines in HEAD `.env.example`. Credential / key material:

| Key | Committed value | Class |
|---|---|---|
| `MT5_PASSWORD` | `<SECRET>` | placeholder |
| `ACHIEVER_PROXY_USERNAME` | `<SECRET>` | placeholder |
| `ACHIEVER_PROXY_PASSWORD` | `<SECRET>` | placeholder |
| `MT5_STARWAVEFX_PASSWORD` | `<SECRET>` | placeholder |
| `CTRADER_FIX_PASSWORD` | `<SECRET>` | placeholder |
| `DATABASE_URL` … `Password=` | `<SECRET>` | placeholder |
| `MT5_PASSWORD_ENCRYPTION_KEY` | `<BASE64_ENCODED_256BIT_KEY>` | placeholder |
| `CTRADER_FIX_QUOTE_SENDER_SUB_ID` | `<BROKER_ISSUED_VALUE>` | placeholder |
| `CTRADER_FIX_QUOTE_TARGET_SUB_ID` | `<BROKER_ISSUED_VALUE>` | placeholder |
| `CTRADER_FIX_TRADE_SENDER_SUB_ID` | `<BROKER_ISSUED_VALUE>` | placeholder |
| `CTRADER_FIX_TRADE_TARGET_SUB_ID` | `<BROKER_ISSUED_VALUE>` | placeholder |

Token counts in the blob: **6** `<SECRET>`, **4** `<BROKER_ISSUED_VALUE>`, **1** `<BASE64_ENCODED_256BIT_KEY>`.

No hex/base64 key material. No quoted live password. `REDIS_URL=localhost:6379` has **no** password field (catalog gap vs A75 `REDIS_PASSWORD=<SECRET>`, not a leak). Empty `ACHIEVER_PROXY_HOST=` / `ACHIEVER_PROXY_PORT=` are blanks, not secrets.

**Answer for “are the secret slots placeholders only?”: YES.**

---

## 4. Identity / targeting values — FAIL (not placeholders)

Three IPv4 literals in the file. Live values match architecture §56 almost verbatim.

| Key | Committed value | A75 required |
|---|---|---|
| `MT5_SERVER` | `57.128.141.65` | `<MT5_MANAGER_HOST>` |
| `MT5_LOGIN` | `2027` | `<MANAGER_LOGIN>` |
| `MT5_DEFAULT_GROUP` | `demo\Maxmaster` | `<PLAN_GROUP_PATH>` |
| `MT5_SERVER_NAME` | `AchieverGlobalMarkets-Server` | `<SERVER_NAME>` |
| `ACHIEVER_EGRESS_IP` | `81.29.145.69` | `<EGRESS_IP>` |
| `MT5_STARWAVEFX_SERVER` | `84.201.6.142` | `<MT5_MANAGER_HOST>` |
| `MT5_STARWAVEFX_LOGIN` | `9904` | `<MANAGER_LOGIN>` |
| `MT5_STARWAVEFX_DISPLAY_NAME` | `StarwaveFX` | `<DISPLAY_NAME>` |
| `MT5_STARWAVEFX_SERVER_NAME` | `StarwaveFX` | `<SERVER_NAME>` |
| `CTRADER_FIX_HOST` | `live-us-eqx-01.p.c-trader.com` | `<CTRADER_FIX_HOST>` |
| `CTRADER_FIX_ACCOUNT_ID` | `1369850` | `<FIX_ACCOUNT_ID>` |
| `CTRADER_FIX_QUOTE_SENDER_COMP_ID` | `live.pepperstone.1369850` | `<BROKER_ISSUED_VALUE>` |
| `CTRADER_FIX_TRADE_SENDER_COMP_ID` | `live.pepperstone.1369850` | `<BROKER_ISSUED_VALUE>` |
| `CTRADER_FIX_*_TARGET_COMP_ID` | `cServer` | `<BROKER_ISSUED_VALUE>` (case must be issued, not guessed) |
| `MT5_GROUP_*` (9 keys) | `demo\yo-*` / `contest\yo-*` | `<PLAN_GROUP_PATH>` |

Delta vs §56 that is **safer**, not a scrub:

- `ACHIEVER_PROXY_ENABLED=false` and empty proxy host/port (architecture sample has proxy **on** at `81.29.145.69:49527`).

These identifiers are **not passwords**. They are still forbidden in a §55 placeholder-only example (A19-02 / A75-01 / B40-02). Publishing them in a tracked file is reconnaissance data: manager endpoints, manager logins, FIX account, CompID.

**Answer for “is the whole file placeholders only?”: NO.**

---

## 5. Safe literals and extra blocks

### 5.1 Allowed literals (not a leak)

| Key class | Values | Why allowed |
|---|---|---|
| Ports | `443`, `5211`, `5201`, `5212`, `5202`, `5432`, `6379` | published defaults |
| Mode / pool | `MT5_MODE=local`, pool sizes `8` / `4` | non-secret knobs |
| SSL / session names | `CTRADER_FIX_USE_SSL=true`, qualifiers `QUOTE`/`TRADE` | protocol |
| Connect-without-trade | `CTRADER_FIX_ENABLED=true`, quote/trade session `true` | §41 |
| Copy floor | `REAL_COPY_EXECUTION_ENABLED=false` | **required** |
| Local data plane hosts | `localhost` Postgres / Redis | generic; password still `<SECRET>` |
| Log knobs | `LOG_LEVEL=info`, `LOG_FORMAT=text` | non-secret |

### 5.2 Extra keys **not** in architecture §56

Still present (A75-07). They do **not** introduce live secrets:

- `RISK_MAX_*`, `RISK_SLIPPAGE_*`, `RISK_COPY_*`, `RISK_EMERGENCY_FLATTEN_ENABLED`, `RISK_KILL_SWITCH_ENABLED`
- `FEATURE_COPY_TRADING_ENABLED=false`, `FEATURE_CTRADER_HEDGING_ENABLED=false`, `FEATURE_ML_SCORING_ENABLED=false`, `FEATURE_NEWS_FILTER_ENABLED=false`, `FEATURE_TRADE_RECONSTRUCTION_ENABLED=true`
- `ASPNETCORE_ENVIRONMENT=Development`
- `MT5_VOLUME_SCALE=10000`
- `DATABASE_URL` / `REDIS_URL` / `MT5_PASSWORD_ENCRYPTION_KEY`
- `CTRADER_FIX_ENABLED` (this one **is** in §41; A75 wants it)

A75 canonical sheet also wants `VITE_API_URL=` (empty) and a named `REDIS_PASSWORD=<SECRET>`. Both are **absent**.

`FEATURE_COPY_TRADING_ENABLED=false` is **not** a substitute for `REAL_COPY_EXECUTION_ENABLED`. The architecture name is present and correctly `false`.

---

## 6. A75 acceptance checklist (applied to HEAD blob)

When I0 writes the example from A75 §4, these boxes must be true. Applied **now** to the committed file:

| Check | HEAD `.env.example` |
|---|---|
| Every §56 key name is present | **YES** (plus extras) |
| `CTRADER_FIX_ENABLED` present | **YES** |
| All four SubID keys = `<BROKER_ISSUED_VALUE>` | **YES** |
| `REAL_COPY_EXECUTION_ENABLED=false` exactly | **YES** |
| No live IP / login / FIX account / SenderCompID / host / egress | **NO** |
| Every password / key slot is `<SECRET>` (or the documented key token) | **YES** |
| Plan-group values are `<PLAN_GROUP_PATH>` | **NO** (live `demo\`/`contest\` paths) |
| `VITE_*` contains no secret | **N/A** (key missing) |
| Postgres / Redis password slots are `<SECRET>` | Postgres **yes**; Redis **no named slot** |
| Product source unchanged by an example-file edit | **N/A this pass** (file not rewritten) |

Score vs A75 “done when”: **secret-slot half pass, identifier half fail.**

---

## 7. Adjacent surfaces (context only; not the assigned file)

| Surface | State | Secrets? | Identifiers? |
|---|---|---|---|
| Architecture §56 | printed in v2 | passwords tokenized | **live** hosts/logins/CompIDs |
| `mt5-sdk/.env.example` | on disk, SHA unchanged vs B40 | `replace_with_manager_password` / empty `DB_PASSWORD` / empty encryption key | fictional `mt5.your-broker.example`, login `1000`, `demo\default`. **No** Starwave / FIX / `REAL_COPY_*` |
| `CTraderFixOptions.cs` | product (not edited here) | `Password = ""` | compiled `Host = live-us-eqx-01.p.c-trader.com`, `SenderCompId = live.pepperstone.1369850`, `TargetCompId = cServer` |
| Host `appsettings.json` | not re-audited in full | B40: no live passwords on HEAD | not this ticket |
| Real `.env` fill | **none** — ignored `.env` **is** the example | none | same as example |

SDK `MT5_REMOTE_URL=http://127.0.0.1:9100` is a local HTTP default (A19-06 / A75: if that line is copied for a remote bridge, use `https://`). Not a live secret.

---

## 8. Working-tree hygiene (not a secret leak)

Someone (or an earlier pass) deleted the tracked example and left the same bytes as `D:\Prop\.env`.

| Risk | Measured |
|---|---|
| Accidental commit of a later **filled** `.env` | **Low** — `.gitignore` matches `.env` |
| Accidental **loss** of the committed template from the working tree | **Now** — `D .env.example` |
| Clone of `origin` still has the identifier-leaking example | **Yes** — blob still on `HEAD` |
| Operator thinks `.env` is private so they paste real passwords into this file | **Possible.** File is ignored, so that would stay local. Do not `git add -f .env`. |

This agent did **not** restore `.env.example` and did **not** delete `.env`. Restoring the tracked name without rewriting content would re-publish the same identifiers. A later I0 should restore the **name** and replace the **body** with A75 §4.

---

## 9. Findings (report-only)

| ID | Sev | Finding |
|---|---|---|
| D61-01 | — | **PASS.** No live MT5 / FIX / proxy / DB / Redis / encryption **password** in the example blob. Secret slots are tokens. `REAL_COPY_EXECUTION_ENABLED=false`. |
| D61-02 | P2 | **FAIL assigned question.** File is **not** placeholder-only. Live IPs `57.128.141.65`, `84.201.6.142`, egress `81.29.145.69`, logins `2027` / `9904`, FIX account `1369850`, host `live-us-eqx-01.p.c-trader.com`, SenderCompID `live.pepperstone.1369850`, live group paths. Same class as A75-01 / B40-02. |
| D61-03 | P3 | Working tree: assigned path **missing**; identical content at ignored `D:\Prop\.env`. Status `D .env.example`. Not a fill; a rename. |
| D61-04 | P3 | Extra `FEATURE_*` / `RISK_*` names are not §56. Harmless as secrets; drop on A75 §4 rewrite. |
| D61-05 | P3 | Catalog gaps vs A75 §4: no `VITE_API_URL`, no `REDIS_PASSWORD`, CompIDs / hosts not tokenized. `CTRADER_FIX_*_TARGET_COMP_ID=cServer` is a guessed case, not `<BROKER_ISSUED_VALUE>`. |

No I0 rewrite performed. Product source was not modified.

---

## 10. Honesty close

**Assigned question: “Placeholders only?” → NO.**

Split, so the NO is not a secret-scan fail:

1. **Passwords / keys:** placeholders only.
2. **Venue identity:** live values from architecture §56.
3. **On-disk path `D:\Prop\.env.example`:** absent; evaluation used `git show HEAD:.env.example`, proven byte-identical to ignored `D:\Prop\.env` (SHA-256 `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA`).

B40’s “secrets are not committed / not identifier-clean” still holds. A75’s recommended replacement (canonical placeholder sheet) was **not** applied.

**Product source was not modified. This file is the only output.**
