# C48 — `apps/web/tailwind.config.js` content globs

| Field | Value |
|---|---|
| Agent | C48 (senior engineer, Tailwind `content` globs only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:28:47+05:30 |
| Assigned | Read `apps/web/tailwind.config.js` content globs. Write this report. Do not modify product source. |
| Workspace | `D:\Prop\src` (assigned relative path `apps/web/tailwind.config.js`) |
| Product tree | Vite app is **`D:\Prop\apps\web`**, **not** under `D:\Prop\src` |
| Product source modified | **No.** This report is the only write. |
| HEAD at measure | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`, 2026-08-18 13:24:21 +0530) |
| Config vs HEAD | Clean — `git status --short -- apps/web/tailwind.config.js` empty |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict

**Content globs are present, official Vite + Tailwind v3 shape, and they cover every product file that currently holds a utility class.**

The assigned workspace-relative path `D:\Prop\src\apps\web\tailwind.config.js` does **not** exist (`Test-Path` = `False`; `D:\Prop\src\apps` is also absent). The single product Tailwind config is:

`D:\Prop\apps\web\tailwind.config.js`

Line 3:

```js
content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
```

Two globs. Relative to the config file directory (`D:\Prop\apps\web`). That is the stock Vite + React + TypeScript + Tailwind **3.4** template. Measured coverage vs the on-disk `src/` tree is complete for class extraction.

| Question | Answer | Class |
|---|---|---|
| Does `D:\Prop\src\apps\web\tailwind.config.js` exist? | **No.** | path trap, not a missing config |
| Does `D:\Prop\apps\web\tailwind.config.js` exist? | **Yes.** 169 B, SHA-256 below | `EXISTS_AND_GOOD` |
| `content` key present? | **Yes.** Array of **2** string globs | `EXISTS_AND_GOOD` |
| `./index.html` covers the HTML shell? | **Yes.** Line 2 `class="dark"`, line 8 `bg-gray-900 text-gray-100` | `EXISTS_AND_GOOD` |
| `./src/**/*.{js,ts,jsx,tsx}` covers `src/` modules? | **Yes.** **25 / 25** `.{js,ts,jsx,tsx}` files | `EXISTS_AND_GOOD` |
| Product files with class tokens **outside** `content`? | **0** | `EXISTS_AND_GOOD` |
| `node_modules` / `public/` scanned? | **No** `public/`; `node_modules` not in `content` | `EXISTS_AND_GOOD` |
| Other `tailwind.config.*` under `D:\Prop` (exclude vendor)? | **This file only** | — |
| Is the dashboard visually complete because globs are good? | **No.** Globs ≠ §46 widgets | — |

Honest one-liner: **Do not recreate or widen `content` for today's tree. Do not look for the config under `D:\Prop\src`.**

Overall `content` class: **`EXISTS_AND_GOOD`**.

`theme` / `plugins` / `darkMode` are out of the assigned question. They are noted in §7 so they are not mistaken for glob defects.

---

## 1. Method

| Source | Path / action |
|---|---|
| Assigned path | `read_file` `D:\Prop\src\apps\web\tailwind.config.js` → **file not found** |
| Actual file | `read_file` `D:\Prop\apps\web\tailwind.config.js` (full 6 content lines) |
| Existence | `Test-Path` on `D:\Prop\src\apps`, `D:\Prop\src\apps\web\tailwind.config.js`, `D:\Prop\apps\web\tailwind.config.js`, `D:\Prop\apps\web\public` |
| Metrics | PowerShell `Get-Item`, `Get-FileHash SHA256`, byte length, raw char length, CRLF, BOM |
| Sibling wiring | `postcss.config.js`, `package.json` (`tailwindcss ^3.4.6`), `src/index.css`, `index.html`, `vite.config.ts` |
| Glob census | `Get-ChildItem` `apps/web/src` recursive; filter `.{js,ts,jsx,tsx}` vs leftover extensions |
| Class tokens | `className` / `class=` under `apps/web` product `*.{html,tsx,ts,js,css}` (exclude `node_modules`) |
| Other configs | recursive `tailwind.config.*` under `D:\Prop`, exclude `node_modules` / `bin` / `obj` / `vendor` |
| Prior mentions | A62 tree (`tailwind.config.js` named, no `content` quote); B10 existence table (**169 B**); C40 sibling list |
| Git | `git rev-parse HEAD`; `git status --short -- apps/web/tailwind.config.js` |

No `npm`, no `npx tailwindcss`, no `vite build`, no product edit.

---

## 2. File identity (disk, this pass)

| Metric | Value |
|---|---|
| Absolute path | `D:\Prop\apps\web\tailwind.config.js` |
| Workspace-relative miss | `D:\Prop\src\apps\web\tailwind.config.js` — **absent** |
| Bytes | **169** |
| Raw chars | **169** (matches byte length; ASCII-only) |
| Physical lines (`-split "`n"`) | **7** (6 content + trailing newline) |
| Newlines | **CRLF** |
| UTF-8 BOM | **No** |
| SHA-256 | `495C463DE811949745332EE05631D6E4F6D15766C3808C556E163EFB380FC71B` |
| Created (UTC) | `2026-08-18T07:36:12.9426048Z` |
| Last write (UTC) | `2026-08-18T07:36:12.9426048Z` |
| Last write (local) | `2026-08-18T13:06:12+05:30` |
| Module form | ESM `export default` — matches `package.json` `"type": "module"` |
| JSDoc type | `/** @type {import('tailwindcss').Config} */` |

B10 recorded this file as **169 B**. Size still matches. This pass adds the SHA-256.

---

## 3. Full file (quoted)

```js
/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: { extend: {} },
  plugins: [],
};
```

Line map:

| Line | Content | Role |
|---|---|---|
| 1 | `/** @type {import('tailwindcss').Config} */` | editor / tsserver hint |
| 2 | `export default {` | ESM config (Tailwind v3 JS API) |
| **3** | **`content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],`** | **assigned: content globs** |
| 4 | `theme: { extend: {} },` | no custom tokens |
| 5 | `plugins: [],` | no `@tailwindcss/forms` / typography |
| 6 | `};` | close |

No `darkMode`, no `safelist`, no `content.files` object form, no `content.extract`, no `important`, no `prefix`, no `corePlugins` override.

---

## 4. The two globs (binding parse)

Tailwind v3 resolves `content` strings with `fast-glob` from the **directory that contains the config**, here `D:\Prop\apps\web`.

| # | Literal | Engine meaning | Resolves under |
|---|---|---|---|
| 1 | `./index.html` | one file, app HTML shell | `D:\Prop\apps\web\index.html` |
| 2 | `./src/**/*.{js,ts,jsx,tsx}` | recursive, brace-expand four extensions | `D:\Prop\apps\web\src\**\*.{js,ts,jsx,tsx}` |

`**` in this stack matches **zero or more** directories, so files sitting directly in `src/` (`App.tsx`, `main.tsx`) are included. That is the official Vite template, not a miss.

Brace set `{js,ts,jsx,tsx}` is **source modules only**. It does **not** include:

| Extension / path | In glob 2? | On disk today? |
|---|---|---|
| `.css` | No | **Yes** — only `src/index.css` (62 B, `@tailwind` directives, **zero** utility tokens) |
| `.html` under `src/` | No | **No** |
| `.mjs` / `.cjs` / `.vue` / `.svelte` / `.mdx` | No | **No** |
| `apps/web/public/**` | No | **`public/` absent** (`Test-Path` false) |
| `apps/web/*.{ts,js}` at root (`vite.config.ts`, this config) | No | Present; no `className` |
| `node_modules/**` | No | Present; correctly **not** scanned |

Glob 1 is required: `index.html` is **outside** `src/`. Without it, `bg-gray-900` / `text-gray-100` on `<body>` would be invisible to the extractor (the React tree paints the same colors on `<main>`, so the body utilities are redundant, but they are still scanned).

---

## 5. Measured coverage vs disk

### 5.1 Glob 1 — `./index.html`

| Check | Result |
|---|---|
| File exists | **Yes.** 369 B, SHA-256 `080656C860AC6F8C1FAB242789DEEF0803EC278028D8B0F24115A14536FDB8FD` (C40) |
| Other `*.html` under `apps/web` (exclude `node_modules`) | **This file only** (C40) |
| Class tokens in it | `dark` (L2), `bg-gray-900 text-gray-100` (L8) |

Covered.

### 5.2 Glob 2 — `src` modules

`Get-ChildItem` of `D:\Prop\apps\web\src` (recursive, files only): **26** files.

| Bucket | Count |
|---:|---|
| Match `.{js,ts,jsx,tsx}` | **25** |
| Do not match (`.css`) | **1** (`src/index.css`) |
| `.js` | **0** |
| `.jsx` | **0** |
| `.ts` | **5** |
| `.tsx` | **20** |

All 25 matching files (bytes this pass):

| Path | Bytes | Has `className` / `class=`? |
|---|---:|---|
| `src/App.tsx` | 2062 | No (router only) |
| `src/main.tsx` | 648 | No (mount only) |
| `src/api/client.ts` | 232 | No |
| `src/api/hooks.ts` | 1935 | No |
| `src/api/signalr.ts` | 899 | No |
| `src/types/index.ts` | 2905 | No |
| `src/utils/formatters.ts` | 947 | No |
| `src/components/MetricCard.tsx` | 521 | **Yes** |
| `src/components/StatusBadge.tsx` | 699 | **Yes** |
| `src/layouts/DashboardLayout.tsx` | 1854 | **Yes** |
| `src/pages/AuditPage.tsx` | 324 | **Yes** |
| `src/pages/BrokersPage.tsx` | 1266 | **Yes** |
| `src/pages/FixSessionsPage.tsx` | 1312 | **Yes** |
| `src/pages/GroupsPage.tsx` | 1228 | **Yes** |
| `src/pages/LiveCopyPage.tsx` | 321 | **Yes** |
| `src/pages/OverviewPage.tsx` | 2078 | **Yes** |
| `src/pages/ReconciliationPage.tsx` | 490 | **Yes** |
| `src/pages/RiskPage.tsx` | 1148 | **Yes** |
| `src/pages/ScoringPage.tsx` | 1288 | **Yes** |
| `src/pages/SettingsPage.tsx` | 459 | **Yes** |
| `src/pages/ShadowPortfolioPage.tsx` | 628 | **Yes** |
| `src/pages/SystemHealthPage.tsx` | 369 | **Yes** |
| `src/pages/TradeExplorerPage.tsx` | 1321 | **Yes** |
| `src/pages/TraderDetailPage.tsx` | 1592 | **Yes** |
| `src/pages/TradersPage.tsx` | 1604 | **Yes** |

**18 / 25** scanned modules carry `className`. The other 7 are still correctly inside the glob (harmless extra scan). **15 / 15** page modules from C08 are inside glob 2.

The one `src/` file **not** in `content` is `src/index.css`:

```css
@tailwind base;
@tailwind components;
@tailwind utilities;
```

Those are **directives**, not candidates. Tailwind does not need `index.css` in `content`. Leaving `.css` out is correct.

### 5.3 Class tokens outside `content`?

Product grep (`className` / `class=`) under `apps/web`, exclude `node_modules`:

| Location | In `content`? |
|---|---|
| `index.html` (2 tags) | **Yes** — glob 1 |
| 18 `src/**/*.tsx` files (101 `className` lines) | **Yes** — glob 2 |
| `src/index.css` | N/A — no class tokens |
| Root `*.js` / `*.ts` configs | No class tokens |
| `public/` | Directory does not exist |

**Zero** utility-bearing product files sit outside the two globs.

---

## 6. Extractor notes (not glob defects)

Tailwind v3 extracts **complete** class string literals. Incomplete templates (`bg-${x}-600`) are dropped. This tree does **not** do that.

| Pattern | Where | Extractable? |
|---|---|---|
| Static `className="…"` | pages, layout, MetricCard | Yes |
| Template with embedded **full** tokens | `DashboardLayout` NavLink `isActive` ternary | Yes — both branches are full strings |
| `Record<string, string>` of full tokens | `StatusBadge.tsx` `colors` (`bg-green-600/20 text-green-300`, …) | Yes — file is `.tsx` in glob 2 |
| Fallback full token | `StatusBadge` `|| 'bg-gray-600/20 text-gray-300'` | Yes |
| `color="text-blue-300"` / `color="text-amber-300"` props | `OverviewPage.tsx`, `RiskPage.tsx` | Yes — literals in those `.tsx` files |
| Ternary full tokens | `OverviewPage` `color={data.mt5Healthy ? 'text-emerald-300' : 'text-red-400'}`; `BrokersPage` `text-emerald-300` / `text-red-400` | Yes |
| `dark:` variant prefix | **0 hits** in product `*.{html,css,js,ts,jsx,tsx}` | n/a |

No `safelist` is required for the current literals.

A62 target folders (`src/auth/`, `src/routes/`, `src/hubs/`, future `Login` / `Models` pages) would still match glob 2 the moment they are created as `.{js,ts,jsx,tsx}`. **Do not add extra globs until a file with utilities lives outside `index.html` + `src/`.**

---

## 7. Adjacent config (not the assigned question)

Recorded so a later agent does not reopen `content` to “fix” these.

| Item | Measured | Relevance to `content` |
|---|---|---|
| Tailwind version | `package.json` `tailwindcss ^3.4.6` (dev) | v3 uses `content` array. v4 `@source` is **not** in play. |
| PostCSS | `postcss.config.js` `tailwindcss: {}` + `autoprefixer: {}` | Wires the config; does not change globs |
| CSS entry | `src/index.css` three `@tailwind` layers | Consumed via import, not via `content` |
| `theme.extend` | `{}` | empty; not a glob issue |
| `plugins` | `[]` | empty; not a glob issue |
| `darkMode` | **absent** → Tailwind default `'media'` | `<html class="dark">` does not enable `dark:` variants. **No `dark:` classes exist today.** Body/main use plain `bg-gray-900`. Not a `content` miss. |
| Vite `root` / multi-page `input` | unset (C40) | default `index.html` = glob 1 |
| A62 keep-list | names `tailwind.config.js`; does not prescribe a different `content` | this file satisfies the name |

PostCSS file (`D:\Prop\apps\web\postcss.config.js`, 87 B):

```js
export default {
  plugins: {
    tailwindcss: {},
    autoprefixer: {},
  },
};
```

---

## 8. Path trap (do not re-open)

| Path | Exists? |
|---|---|
| `D:\Prop\src\apps\web\tailwind.config.js` | **No** |
| `D:\Prop\src\apps` | **No** |
| `D:\Prop\apps\web\tailwind.config.js` | **Yes** |
| Other `tailwind.config.*` under `D:\Prop` (exclude `node_modules` / `bin` / `obj` / `vendor`) | **This file only** |

C08 / C40 already noted: looking only at the C# workspace `D:\Prop\src` is a false miss. Same trap for this file.

---

## 9. What this does **not** prove

This check is **`content` globs only**.

- Does **not** prove pages / A26 `/api/v1/**` / SignalR `/hubs/ops` / ECharts / login.
- Does **not** re-litigate C08 page census or B10 widget gap.
- Does **not** authorize rewriting `tailwind.config.js`.
- Does **not** claim a `vite build` / JIT CSS emit was run this pass.
- Empty `theme.extend` and missing `darkMode: 'class'` are **not** glob failures.

---

## 10. Binding answer

**Content globs:** `['./index.html', './src/**/*.{js,ts,jsx,tsx}']` at `D:\Prop\apps\web\tailwind.config.js` line 3.

They cover the HTML shell and every `src` JS/TS/JSX/TSX module. Every current utility-bearing file is inside that set. Product source was not modified.
