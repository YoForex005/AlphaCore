# R005 — Secret locations (`MT5_PASSWORD` in `.env` / `appsettings`)

| Field | Value |
|---|---|
| Agent | R005 |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T08:26:34Z (`.env` stamps) / 2026-08-18T08:28Z (this pass) |
| Host | `DESKTOP-FQPFPKE` / user `ADMIN` |
| Artifact | `D:\Prop\reports\swarm\20260818\R005_secret_locations.md` |
| Assigned | Search `D:\Prop` and sibling folders for files named `.env` or `appsettings` that contain `MT5_PASSWORD`. Report **only path** and **PLACEHOLDER vs PRESENT**. Do not write the password. Do not modify product source. |
| Product source modified | **No.** This report is the only write. |
| Config / `.env*` / `appsettings` edited | **No.** |
| Secret values printed | **None.** |

**Masking rule:** the `MT5_PASSWORD` value is never copied. Classification is path + `PLACEHOLDER` / `PRESENT` only.

---

## 0. Assigned table (files that contain `MT5_PASSWORD`)

| Path | Class |
|---|---|
| `D:\Prop\.env` | **PRESENT** |
| `D:\Prop\mt5-sdk\.env.example` | **PLACEHOLDER** |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\.env` | **PRESENT** |

No `appsettings*` file under `D:\Prop` or scanned sibling trees contains the key `MT5_PASSWORD`.

---

## 1. Classification rules

| Class | Meaning |
|---|---|
| **PRESENT** | File is named `.env` / `.env.*` / `appsettings*`; an assignment `MT5_PASSWORD=` (or JSON `"MT5_PASSWORD"`) exists; value is non-empty and is **not** a sentinel (`<SECRET>`, other `<ANGLE>` tokens, `replace_with_*`, empty). |
| **PLACEHOLDER** | Key exists; value is a known sentinel (`replace_with_*`, `<ANGLE>`, empty). |
| *(not listed)* | File exists but has **no** `MT5_PASSWORD` key, or the path is not named `.env` / `appsettings`. |

`MT5_PASSWORD_ENCRYPTION_KEY` is a **different** key and is not counted as `MT5_PASSWORD`.

---

## 2. Scope

| Included | Excluded |
|---|---|
| `D:\Prop` | `node_modules\`, `.git\`, `Games\`, `Program Files\`, `WindowsApps\`, `WpSystem\`, `$RECYCLE.BIN` |
| Sibling roots under `D:\` (same volume): `Projects`, `AUJ Frontend`, `AUJ New Frontend`, `Ex5*`, `NEW EX5`, `TP-SL-`, `TPSL EA`, `tmp`, `F`, `mco-reports`, `bb_return_test`, `config`, `Discord`, `NXV_20260801T1544Z`, `OneDrive - yoaccount`, `OneDriveTemp` | Product source edits |

Method: filename hunt (`.env`, `.env.*`, `appsettings*.json`) + local classify (value discarded after class). Content grep for the key name `MT5_PASSWORD` on those names. Content-search tools may redact live slots as `<SECRET>` — class is taken from a local parse, not from that redaction.

---

## 3. `appsettings` (named, no key)

These exist under `D:\Prop` and do **not** contain `MT5_PASSWORD` (not in the assigned table):

- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\apps\api\appsettings.Development.json`
- `D:\Prop\apps\mt5-worker\appsettings.json`
- `D:\Prop\apps\mt5-worker\appsettings.Development.json`
- `D:\Prop\apps\fix-worker\appsettings.json`
- `D:\Prop\apps\fix-worker\appsettings.Development.json`

No sibling `appsettings*.json` with `MT5_PASSWORD` was found.

---

## 4. Sibling `.env*` seen without the key (not in assigned table)

| Path | `MT5_PASSWORD` |
|---|---|
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\.env.example` | no key |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\local-runtime\.env` | no key |
| `D:\Projects\YoPips\Forntend\yopips_Frontend_Remasterd\admin-dashboard\.env` | no key |
| `D:\Projects\YoPips\Forntend\yopips_Frontend_Remasterd\admin-dashboard\.env.example` | no key |
| `D:\Projects\YoPips\Forntend\yopips_Frontend_Remasterd\admin-dashboard\.env.local` | no key |
| `D:\Projects\YoPips\Forntend\yopips_Frontend_Remasterd\admin-dashboard\.env.production` | no key |
| `D:\Projects\YoPips\Forntend\yopips_Frontend_Remasterd\client\.env.example` | no key |
| `D:\Projects\YoPips\Forntend\yopips_Frontend_Remasterd\client\.env.production` | no key |
| `D:\AUJ Frontend\backend\vendor\rtx5-go-sdk\.env.example` | no key |

`D:\Prop\.env.example` and `D:\Prop\mt5-sdk\.env` are **absent**.

---

## 5. Honesty

- **Two** `.env` files on `D:\` carry a **PRESENT** `MT5_PASSWORD` slot: gitignored `D:\Prop\.env`, and sibling `D:\Projects\YoPips\Backend\C++ Backend PropFirm\.env`.
- The only **PLACEHOLDER** slot in a `.env*` name is `D:\Prop\mt5-sdk\.env.example`.
- Product `appsettings` do **not** carry `MT5_PASSWORD`.
- `E001` / `D40` “placeholder-only `D:\Prop\.env`” are **stale**. `R001` also classed `D:\Prop\.env` as **PRESENT** (this pass: still **PRESENT** after a later rewrite of that file).
- This report does **not** prove a live Manager session.
- No secret value is written here.
