# C40 — `apps/web/index.html` root-div check

| Field | Value |
|---|---|
| Agent | C40 (senior engineer, Vite HTML shell only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (hash + `Get-Item` this pass) |
| Workspace | `D:\Prop\src` (assigned relative path `apps/web/index.html`) |
| Product tree | Vite app is **`D:\Prop\apps\web`**, **not** under `D:\Prop\src` |
| Assigned question | Read `apps/web/index.html`. Has root div? Write this report. |
| Product source modified | **No.** This report is the only write. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict

**YES. `index.html` has a root div: `<div id="root"></div>` (line 9).**

The assigned workspace-relative path `D:\Prop\src\apps\web\index.html` does **not** exist (`Test-Path` = `False`; `D:\Prop\src\apps` is also absent). The single product HTML shell is:

`D:\Prop\apps\web\index.html`

That file is a 12-line Vite entry. Body contains exactly one element besides the module script: an empty `div` with `id="root"`. `src/main.tsx` mounts React 18 onto that node via `document.getElementById('root')`.

| Question | Answer | Evidence |
|---|---|---|
| Does `D:\Prop\src\apps\web\index.html` exist? | **No.** | `Test-Path` false; no `apps/` under `src/` |
| Does `D:\Prop\apps\web\index.html` exist? | **Yes.** | 369 bytes, SHA-256 below |
| Has a root `div`? | **Yes.** | Line 9: `<div id="root"></div>` |
| `id="root"` count in that file | **1** | no duplicate mount nodes |
| Other `*.html` under `apps/web` (exclude `node_modules`) | **1** | this file only |
| React mount target matches? | **Yes.** | `main.tsx` L14 `getElementById('root')` |
| SPA host complete enough to boot Vite? | **Yes** (HTML) | script tag `/src/main.tsx` present |
| Is this a finished dashboard? | **No.** | HTML is only the mount shell |

Honest one-liner: **Root div is present and correctly id'd. Do not recreate `index.html`. Do not look for it under `D:\Prop\src`.**

Overall HTML-shell class: **`EXISTS_AND_GOOD`** for the Vite React mount contract.

---

## 1. Method

| Source | Path / action |
|---|---|
| Assigned path | `read_file` `D:\Prop\src\apps\web\index.html` → **file not found** |
| Actual file | `read_file` `D:\Prop\apps\web\index.html` (full 12 lines) |
| Mount proof | `read_file` `D:\Prop\apps\web\src\main.tsx` L14 |
| Vite config | `D:\Prop\apps\web\vite.config.ts` (default `index.html` root; no custom `root` / `build.rollupOptions.input`) |
| Existence | `Test-Path` on `D:\Prop\src\apps\web\index.html` and `D:\Prop\src\apps` |
| Metrics | PowerShell `Get-Item`, `Get-FileHash SHA256`, physical-line count, raw char length, newline style |
| Other HTML | `Get-ChildItem -Recurse -Filter *.html` under `D:\Prop\apps\web`, exclude `node_modules` |
| Grep | `id=["']root["']` and `getElementById` in `apps/web` `*.{html,tsx,ts,jsx,js}` |
| Prior mentions | A62 §0 / tree (369 B, title quoted); B10 existence table (same 369 B) |

No `npm`, no `tsc`, no `vite build`, no product edit.

---

## 2. File identity (disk, this pass)

| Metric | Value |
|---|---|
| Absolute path | `D:\Prop\apps\web\index.html` |
| Workspace-relative miss | `D:\Prop\src\apps\web\index.html` — **absent** |
| Bytes | **369** |
| Physical lines | **12** |
| Raw chars | **369** (matches byte length; ASCII-only) |
| Newlines | **CRLF** |
| SHA-256 | `080656C860AC6F8C1FAB242789DEEF0803EC278028D8B0F24115A14536FDB8FD` |
| Created (UTC) | `2026-08-18T07:36:06.3880447Z` |
| Last write (UTC) | `2026-08-18T07:36:06.3890457Z` |
| Last write (local) | `2026-08-18T13:06:06+05:30` |
| Sibling files in `apps/web` (root, not dirs) | `index.html`, `package.json` (739 B), `package-lock.json`, `postcss.config.js`, `tailwind.config.js`, `tsconfig.json`, `tsconfig.node.json`, `vite.config.ts` |

A62 and B10 both recorded this file as **369 B** with title `MT5 Trader Intelligence`. That size still matches. This pass adds the SHA-256.

---

## 3. Full file (quoted)

```html
<!DOCTYPE html>
<html lang="en" class="dark">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>MT5 Trader Intelligence</title>
  </head>
  <body class="bg-gray-900 text-gray-100">
    <div id="root"></div>
    <script type="module" src="/src/main.tsx"></script>
  </body>
</html>
```

Line map:

| Line | Content | Role |
|---|---|---|
| 1 | `<!DOCTYPE html>` | HTML5 |
| 2 | `<html lang="en" class="dark">` | English + Tailwind `dark` class on root html |
| 3–7 | `<head>` | charset UTF-8, viewport, title |
| 8 | `<body class="bg-gray-900 text-gray-100">` | dark shell (matches A62 “keep and extend”) |
| **9** | **`<div id="root"></div>`** | **React mount node — the assigned question** |
| 10 | `<script type="module" src="/src/main.tsx"></script>` | Vite ESM entry |
| 11–12 | close body / html | |

---

## 4. Root-div checks (measured)

| Check | Result | Class |
|---|---|---|
| Empty mount `div` present | **Yes** L9 | `EXISTS_AND_GOOD` |
| Attribute is `id="root"` (not `app`, `#app`, `react-root`) | **Yes** | `EXISTS_AND_GOOD` |
| Div is empty (no leftover template markup) | **Yes** | `EXISTS_AND_GOOD` |
| Nested inside `<body>` | **Yes** | `EXISTS_AND_GOOD` |
| Second mount node (`#app`, `#root` duplicate) | **No** | — |
| Noscript fallback | **Absent** | not required by Vite / A62 |
| Favicon / apple-touch / OG tags | **Absent** | not a §69 gate |
| CSP / integrity on script | **Absent** | Vite dev module URL; expected |
| `lang` + dark class | Present on `<html>` | `EXISTS_AND_GOOD` (theme hook) |
| Title | `MT5 Trader Intelligence` | matches A62 keep-list |

Grep under `D:\Prop\apps\web` (product `*.html` / `*.tsx` / `*.ts`, not `node_modules`):

| Pattern | File:line |
|---|---|
| `id="root"` | `apps/web/index.html:9` |
| `getElementById('root')` | `apps/web/src/main.tsx:14` |

No other `id="root"` in the web product tree.

---

## 5. Consumer: `main.tsx` mount

`D:\Prop\apps\web\src\main.tsx` — 648 bytes, SHA-256 `25A2B880FDD5D6831E5DABA65F7078E4D35C263B8BBEC2B6AC1391F7EF647FB3`, last write `2026-08-18T13:06:39+05:30`.

```ts
ReactDOM.createRoot(document.getElementById('root')!).render(
```

That is the standard React 18 + Vite contract:

1. HTML provides `#root`.
2. `createRoot` takes that element (non-null assertion).
3. App tree is `StrictMode` → `QueryClientProvider` → `BrowserRouter` → `App`.

If `#root` were missing, boot would throw at `createRoot(null)`. It is not missing.

`vite.config.ts` does not override `root` or multi-page `input`. Vite therefore serves this `index.html` as the default app entry on port **3000**.

---

## 6. Path trap (do not re-open)

| Path | Exists? |
|---|---|
| `D:\Prop\src\apps\web\index.html` | **No** |
| `D:\Prop\src\apps` | **No** |
| `D:\Prop\apps\web\index.html` | **Yes** |
| `D:\Prop\apps\web\src\index.html` | **No** (not searched as a second shell; only one `*.html` in the app) |

C08 already noted: looking only at the C# workspace `D:\Prop\src` is a false miss. Same trap for this file.

---

## 7. What this does **not** prove

This check is HTML-only.

- Does **not** prove pages / A26 `/api/v1/**` / SignalR `/hubs/ops` / ECharts / login.
- Does **not** re-litigate C08 page census or B20 widget gap.
- Does **not** authorize rewriting `index.html`.
- A62 “keep and extend: `index.html` (dark)” remains valid. The file already has `class="dark"` on `<html>` and the dark body utilities.

---

## 8. Binding answer

**Has root div? Yes.**

`<div id="root"></div>` at `D:\Prop\apps\web\index.html` line 9. React mounts to it. Product source was not modified.
)
