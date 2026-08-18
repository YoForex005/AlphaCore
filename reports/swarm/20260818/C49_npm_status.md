# C49 — `apps/web` npm lockfile and `node_modules` status

| Field | Value |
|---|---|
| Agent | C49 (senior engineer, npm install-state only) |
| Date | 2026-08-18 |
| Measured at (UTC) | 2026-08-18T08:05:00Z (file mtimes older; census this pass) |
| Workspace | `D:\Prop` (Vite app is **`D:\Prop\apps\web`**, not under `D:\Prop\src`) |
| Assigned question | Does `apps/web` have `package-lock` and `node_modules`? Write this report. |
| Product source modified | **No.** This report is the only write. No `npm install` / `npm ci` / `npm run *` that writes `dist/`. |
| Relates | A62, A65, A103, B10, B40, C08, C40 |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

---

## 0. Verdict

**Yes. Both exist.**

| Artifact | Path | Status |
|---|---|---|
| npm lockfile | `D:\Prop\apps\web\package-lock.json` | **EXISTS** — lockfileVersion **3**, 128 411 bytes, SHA-256 below |
| Install tree | `D:\Prop\apps\web\node_modules\` | **EXISTS** — 7 856 files / 880 dirs / **94.36 MiB**; `npm ls --depth=0` is clean |
| Manifest | `D:\Prop\apps\web\package.json` | **EXISTS** (tracked) — 7 deps + 8 devDeps |
| Alternate lockfiles | `yarn.lock`, `pnpm-lock.yaml`, `bun.lock` / `bun.lockb`, `npm-shrinkwrap.json` | **MISSING** (correct — this is an npm tree) |
| Other `node_modules` under `D:\Prop` | — | **None** besides `apps\web\node_modules` |
| Other `package-lock.json` under `D:\Prop` (outside `node_modules`) | — | **This file only** |

B10 §0 / §2 (`apps/web/package-lock.json` and `apps/web/node_modules` **Absent**, `tsc && vite build` cannot run as-is) is **stale**. A65’s sentence “There is **no** `package-lock.json` today” is **stale**. C40 already listed `package-lock.json` as a sibling of `index.html`.

This is **not** a claim that the dashboard builds, that CI can `npm ci`, or that the chart stack matches A62 (ECharts). Those are separate questions. Measured install state:

| Check | Result | §73.B |
|---|---|---|
| `package-lock.json` on disk | **Yes** | **EXISTS_AND_GOOD** (file) |
| Lockfile committed | **No** — `git status` = `?? apps/web/package-lock.json`; not ignored | **EXISTS_NEEDS_REFACTOR** (repo hygiene) |
| `node_modules` on disk | **Yes** | **EXISTS_AND_GOOD** (local machine) |
| `node_modules` committed | **No** — `.gitignore:47` `node_modules/` | **EXISTS_AND_GOOD** (correct ignore) |
| Declared packages installed | **15 / 15**; lock version **=** installed version | **EXISTS_AND_GOOD** |
| `npm ls --depth=0` | Clean tree, exit 0 | **EXISTS_AND_GOOD** |
| Toolchain on PATH | `node` **v24.18.0**, `npm` **11.16.0**; no yarn/pnpm/bun | — |
| `echarts` / `echarts-for-react` | **Not** in lock or `node_modules` | **MISSING** vs A62 (expected; not an install-hole) |

**Clone-from-git implication:** a clean checkout **does** get `package.json` and **does not** get `package-lock.json` or `node_modules`. Local `npm run build` can run **on this machine** because the tree is already installed. CI `npm ci` from HEAD **cannot** until the lockfile is added.

---

## 1. Method

Read-only. Product source under `apps/web/src` and `package.json` was **not** edited.

1. `list_dir` + `Get-ChildItem -Force` of `D:\Prop\apps\web` (including hidden).
2. SHA-256, byte length, BOM, CRLF of `package.json` and `package-lock.json`.
3. Node `JSON.parse` of the lockfile (PowerShell `ConvertFrom-Json` rejects the empty `packages[""]` key — expected for lockfileVersion 3).
4. Recurse census of `node_modules` (file/dir/byte counts); `.bin` stems; `.package-lock.json` metadata.
5. Compare `package.json` ranges ↔ `packages["node_modules/<name>"].version` ↔ installed `<name>/package.json` version.
6. `npm ls --depth=0` and `npm ls --package-lock-only --depth=0` (no install). `npm outdated --long` (read-only; exit 1 when Latest > Current is npm’s normal code).
7. `git ls-files`, `git status --short`, `git check-ignore -v --no-index`, `git log -1` for the two files.
8. Recurse for other lockfiles / other `node_modules` directories (exclude contents *inside* `node_modules`).
9. Confirm `.gitignore` line 47 and PATH (`node` / `npm` / `yarn` / `pnpm` / `bun`).

Did **not** run `npm install`, `npm ci`, `npm run build`, or `vite`.

---

## 2. Top-level `D:\Prop\apps\web`

| Name | Kind | Bytes | LastWriteUtc | Notes |
|---|---|---|---|---|
| `package.json` | file | **739** | 2026-08-18T07:36:29.6945552Z | Tracked since `6c41447` |
| `package-lock.json` | file | **128 411** | 2026-08-18T07:53:40.7222257Z | Untracked |
| `node_modules\` | dir | — | 2026-08-18T07:53:40.7417745Z | Ignored; mtime matches lock (same `npm install`) |
| `src\` | dir | — | 2026-08-18T07:36:42Z | Product TS/TSX (not modified this pass) |
| `index.html` | file | 369 | 2026-08-18T07:36:06Z | See C40 |
| `postcss.config.js` | file | 87 | 2026-08-18T07:36:07Z | |
| `tailwind.config.js` | file | 169 | 2026-08-18T07:36:12Z | |
| `tsconfig.json` | file | 585 | 2026-08-18T07:36:17Z | |
| `tsconfig.node.json` | file | 223 | 2026-08-18T07:36:15Z | |
| `vite.config.ts` | file | 169 | 2026-08-18T07:36:19Z | |

**Absent at this root:** `.npmrc`, `.yarnrc`, `.yarnrc.yml`, `yarn.lock`, `pnpm-lock.yaml`, `bun.lock`, `bun.lockb`, `npm-shrinkwrap.json`, `package-lock.json.bak`, `.env`, `Dockerfile`.

---

## 3. `package.json` (manifest)

| Field | Value |
|---|---|
| Path | `D:\Prop\apps\web\package.json` |
| Bytes | **739** |
| Physical lines | **30** (CRLF, no BOM) |
| SHA-256 | `F76288B73111845848A5961BFEBEE40B887EAD40E2E35C5773D149443432B7D6` |
| `name` | `mt5-trader-intelligence` |
| `version` | `1.0.0` |
| `private` | `true` |
| `type` | `module` |
| `packageManager` / `engines` | **Absent** |
| Scripts | `dev` = `vite`; `build` = `tsc && vite build`; `preview` = `vite preview` |
| Git | **Tracked.** `git log -1`: `6c414477f632416031b851171d3354fe2a232594` 2026-08-18 13:12:17 +0530 *Initial commit*. Worktree = no `M` on this file. |

Declared packages (caret ranges only):

| Kind | Name | Range |
|---|---|---|
| dep | `@microsoft/signalr` | `^8.0.0` |
| dep | `@tanstack/react-query` | `^5.51.0` |
| dep | `axios` | `^1.7.0` |
| dep | `react` | `^18.3.1` |
| dep | `react-dom` | `^18.3.1` |
| dep | `react-router-dom` | `^6.26.0` |
| dep | `recharts` | `^2.12.0` |
| dev | `@types/react` | `^18.3.3` |
| dev | `@types/react-dom` | `^18.3.0` |
| dev | `@vitejs/plugin-react` | `^4.3.0` |
| dev | `autoprefixer` | `^10.4.19` |
| dev | `postcss` | `^8.4.39` |
| dev | `tailwindcss` | `^3.4.6` |
| dev | `typescript` | `^5.5.3` |
| dev | `vite` | `^5.3.4` |

---

## 4. `package-lock.json` (lockfile)

| Field | Value |
|---|---|
| Path | `D:\Prop\apps\web\package-lock.json` |
| Exists | **Yes** |
| Bytes | **128 411** |
| Physical lines | **3 635** |
| Line endings | **CRLF only** (0 lone CR, 0 lone LF). First three bytes `7B-0D-0A` (`{\r\n`) |
| UTF-8 BOM | **No** |
| SHA-256 | `72A7570A2E43C80146482FC3701E727ABB6BABEEAC08EFC032031AAE0CB4D7BE` |
| `lockfileVersion` | **3** (npm 7+ / current npm 11) |
| `name` / `version` | `mt5-trader-intelligence` / `1.0.0` (matches manifest) |
| `requires` | `true` |
| `packages` keys | **265** (1 root `""` + **264** `node_modules/*`) |
| `resolved` URLs | **264** (every non-root entry; all `https://registry.npmjs.org/...`) |
| `integrity` hashes | **264** |
| Root `dependencies` / `devDependencies` | **Exact match** to `package.json` (7 + 8) |
| Git | **Untracked** (`??`). **Not** ignored (`git check-ignore` empty). Never committed (`git log` empty). |

Header as on disk:

```json
{
  "name": "mt5-trader-intelligence",
  "version": "1.0.0",
  "lockfileVersion": 3,
  "requires": true,
  "packages": {
    "": {
      "name": "mt5-trader-intelligence",
      "version": "1.0.0",
      "dependencies": { ... },
      "devDependencies": { ... }
    },
```

`npm ls --package-lock-only --depth=0` resolves the same 15 top-level versions as the on-disk tree. The lockfile is internally consistent with the manifest.

---

## 5. `node_modules` (install tree)

| Field | Value |
|---|---|
| Path | `D:\Prop\apps\web\node_modules\` |
| Exists | **Yes** (directory) |
| Recurse entries | **8 736** (7 856 files + 880 dirs) |
| Bytes of files | **98 939 407** (**94.36 MiB**) |
| Top-level entries | **171** (169 non-dot package dirs + `.bin` + `.package-lock.json`) |
| Scoped top-level dirs | **12**: `@alloc`, `@babel`, `@esbuild`, `@jridgewell`, `@microsoft`, `@nodelib`, `@remix-run`, `@rolldown`, `@rollup`, `@tanstack`, `@types`, `@vitejs` |
| `.bin` | **66** files, **22** unique stems including **`vite`**, **`tsc`**, **`tailwindcss`**, **`esbuild`**, `rollup` |
| `node_modules/.package-lock.json` | **EXISTS** — 104 009 bytes, SHA-256 `7101DCF96E972FFB233D7C2BB08034207686D63905C351FE5F2A9ADA226AB3F4`, lockfileVersion 3 (npm’s hidden install stamp; not the project lockfile) |
| `.pnpm` / `.modules.yaml` / `.yarn-integrity` | **MISSING** (not a pnpm/yarn tree) |
| Git | **Ignored.** `git check-ignore -v --no-index` → `.gitignore:47:node_modules/` → `apps/web/node_modules`. `git status --ignored` shows `!! apps/web/node_modules/`. |

Critical bins present:

| Bin | Path | Present |
|---|---|---|
| `vite.cmd` | `D:\Prop\apps\web\node_modules\.bin\vite.cmd` | **Yes** |
| `tsc.cmd` | `D:\Prop\apps\web\node_modules\.bin\tsc.cmd` | **Yes** |
| `tailwindcss.cmd` | `D:\Prop\apps\web\node_modules\.bin\tailwindcss.cmd` | **Yes** |
| `esbuild.cmd` | `D:\Prop\apps\web\node_modules\.bin\esbuild.cmd` | **Yes** |

`list_dir` summarized `node_modules` as “7724 files in subtree”. That listing is gitignore-aware / type-summarized. The PowerShell recurse (**7 856 files**) is the measured count.

---

## 6. Declared vs lock vs installed

`npm ls --depth=0` (live tree) and `npm ls --package-lock-only --depth=0` print the same 15 packages. Every lock version equals the installed `node_modules/<name>/package.json` `version`. All satisfy the caret range in `package.json` (`Current` = `Wanted` in `npm outdated`).

| Package | Kind | `package.json` | Lock | Installed | Match |
|---|---|---|---|---|---|
| `@microsoft/signalr` | dep | `^8.0.0` | 8.0.29 | 8.0.29 | Yes |
| `@tanstack/react-query` | dep | `^5.51.0` | 5.101.4 | 5.101.4 | Yes |
| `axios` | dep | `^1.7.0` | 1.19.0 | 1.19.0 | Yes |
| `react` | dep | `^18.3.1` | 18.3.1 | 18.3.1 | Yes |
| `react-dom` | dep | `^18.3.1` | 18.3.1 | 18.3.1 | Yes |
| `react-router-dom` | dep | `^6.26.0` | 6.30.4 | 6.30.4 | Yes |
| `recharts` | dep | `^2.12.0` | 2.15.4 | 2.15.4 | Yes |
| `@types/react` | dev | `^18.3.3` | 18.3.31 | 18.3.31 | Yes |
| `@types/react-dom` | dev | `^18.3.0` | 18.3.7 | 18.3.7 | Yes |
| `@vitejs/plugin-react` | dev | `^4.3.0` | 4.7.0 | 4.7.0 | Yes |
| `autoprefixer` | dev | `^10.4.19` | 10.5.4 | 10.5.4 | Yes |
| `postcss` | dev | `^8.4.39` | 8.5.26 | 8.5.26 | Yes |
| `tailwindcss` | dev | `^3.4.6` | 3.4.19 | 3.4.19 | Yes |
| `typescript` | dev | `^5.5.3` | 5.9.3 | 5.9.3 | Yes |
| `vite` | dev | `^5.3.4` | 5.4.21 | 5.4.21 | Yes |

`npm outdated --long` (exit 1 = “something has a newer Latest”, not a broken tree):

| Package | Current = Wanted | Latest (unconstrained) |
|---|---|---|
| `@microsoft/signalr` | 8.0.29 | 10.0.11 |
| `react` / `react-dom` | 18.3.1 | 19.2.8 |
| `react-router-dom` | 6.30.4 | 7.18.2 |
| `recharts` | 2.15.4 | 3.10.1 |
| `tailwindcss` | 3.4.19 | 4.3.3 |
| `typescript` | 5.9.3 | 7.0.2 |
| `vite` | 5.4.21 | 8.2.1 |
| `@vitejs/plugin-react` | 4.7.0 | 6.0.5 |
| `@types/react` | 18.3.31 | 19.2.18 |
| `@types/react-dom` | 18.3.7 | 19.2.4 |

Packages not listed by `npm outdated` (`axios`, `@tanstack/react-query`, `autoprefixer`, `postcss`) are already at the latest that satisfies the range **and** match Latest, or npm omitted them. **Do not** bump majors as part of this report.

A62 still wants **ECharts**. Measured: `node_modules/echarts` **no**, `node_modules/echarts-for-react` **no**, `node_modules/recharts` **yes**. That is a product-stack gap, not a missing install of a declared dep.

---

## 7. Git and ignore

Root `.gitignore` (B40 / A103) includes:

```gitignore
# Node / React
node_modules/
dist/
apps/web/.vite/
```

| Path | Tracked? | Ignored? | Working tree |
|---|---|---|---|
| `apps/web/package.json` | **Yes** (initial commit) | No | Clean |
| `apps/web/package-lock.json` | **No** | **No** | `??` untracked |
| `apps/web/node_modules/` | **No** | **Yes** (line 47) | `!!` |

Tracked `apps/web` files (`git ls-files -- apps/web`) are the Vite scaffold (`index.html`, `package.json`, configs, a subset of `src/`). The lockfile is **not** among them.

A later authorized commit should add `package-lock.json` so `npm ci` is possible. This agent did **not** `git add` it.

---

## 8. Host toolchain

| Tool | PATH | Version |
|---|---|---|
| `node` | `C:\Program Files\nodejs\node.exe` | **v24.18.0** |
| `npm` | `C:\Program Files\nodejs\npm.ps1` | **11.16.0** |
| `npx` | `C:\Program Files\nodejs\npx.ps1` | **11.16.0** |
| `yarn` | — | **MISSING** |
| `pnpm` | — | **MISSING** |
| `bun` | — | **MISSING** |
| npm registry | `npm config get registry` | `https://registry.npmjs.org/` |

Vite’s lock entry requires `"node": "^18.0.0 \|\| >=20.0.0"`. Host Node 24.18.0 satisfies that. TypeScript requires `>=14.17`. No `engines` field on the app itself.

---

## 9. Stale reports (do not re-cite as current disk)

| Report | Claim | This pass |
|---|---|---|
| B10 §0 / §2 / § inventory | `package-lock.json` and `node_modules` **Absent**; build cannot run as-is | Both **present**; local bins exist. Build **not re-run** here. |
| A65 Dockerfile notes | “There is **no** `package-lock.json` today” | File exists (untracked). `COPY package.json package-lock.json* ./` + `npm ci` would work **if** the lockfile is in the build context. There is still **no** Dockerfile (B37). |
| C40 §2 sibling list | `package-lock.json` listed next to `index.html` | Confirmed. |

---

## 10. What this does **not** prove

- `npm run build` / `tsc && vite build` was **not** executed. Bins exist; compile success is C-series / later work.
- Dashboard completeness vs A26 / A62 / architecture §§46–54 (see C08, B20). Install state ≠ product done.
- Reproducible CI: lockfile must be **committed** first.
- No Docker / nginx story (B37).

---

## 11. Answer (one line)

**`D:\Prop\apps\web` has both `package-lock.json` (lockfileVersion 3, untracked) and a complete `node_modules` tree (gitignored); B10’s “absent” snapshot is stale.**
