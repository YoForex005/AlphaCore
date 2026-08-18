# W500_SLICE_69 — GroupsPage vs env-before-CreateBuilder

| Field | Value |
|---|---|
| Slot | 69 |
| File | `D:/Prop/apps/web/src/pages/GroupsPage.tsx` |
| Angle | env file not loaded before `WebApplication.CreateBuilder` |
| Date | 2026-08-18 |
| Method | `read_file` of the assigned file (full 34 lines) + `grep` on that file for `CreateBuilder` / `WebApplication` / `AddEnvironmentVariables` / `.env` / `LoadEnvironment` / `dotenv` / `IConfiguration`; `grep` on `D:/Prop/apps/web` for `CreateBuilder` |
| Product source modified | **No** |
| Verdict | **PASS** (empty PASS — defect class does not apply to this file; file was fully read) |

---

## 1. What was read

`GroupsPage.tsx` is a 34-line React function component. Entire file (read, not inferred):

```1:34:D:/Prop/apps/web/src/pages/GroupsPage.tsx
import { useGroups } from '../api/hooks';

export default function GroupsPage() {
  const { data = [], isLoading } = useGroups();
  if (isLoading) return <p className="text-gray-400">Loading groups…</p>;
  return (
    <div>
      <h1 className="text-2xl font-semibold text-white mb-2">MT5 Groups</h1>
      <p className="text-sm text-gray-400 mb-4">Discovered dynamically. Plan mappings are labels only — they do not filter ingestion.</p>
      <table className="w-full text-sm text-left">
        <thead className="text-gray-400 border-b border-gray-800">
          <tr>
            <th className="py-2">Broker</th>
            <th>Group</th>
            <th>Accounts</th>
            <th>Analysis</th>
            <th>Plan</th>
          </tr>
        </thead>
        <tbody>
          {data.map((g: any) => (
            <tr key={`${g.broker}-${g.group}`} className="border-b border-gray-800 text-gray-200">
              <td className="py-2">{g.broker}</td>
              <td>{g.group}</td>
              <td>{g.accounts}</td>
              <td>{g.enabledForAnalysis ? 'yes' : 'no'}</td>
              <td>{g.planMapping ?? '—'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
```

`grep` on `D:/Prop/apps/web/src/pages/GroupsPage.tsx` for `CreateBuilder|WebApplication|AddEnvironmentVariables|\.env|LoadEnvironment|dotenv|IConfiguration` → **0 matches**.

`grep` on `D:/Prop/apps/web` for `CreateBuilder` / `env file not loaded` → **0 matches**.

---

## 2. Angle check

The assigned defect is an **ASP.NET Core host bootstrap** ordering bug: a dotenv / `.env` file must be applied to the process **before** `WebApplication.CreateBuilder(args)` so `IConfiguration` sees `MT5_*` / FIX / connection-string keys.

That pattern can exist only in a .NET host `Program.cs` (or equivalent). This file is Vite/React UI:

- No `Microsoft.AspNetCore` types
- No `WebApplication` / `CreateBuilder` / `Host.CreateApplicationBuilder`
- No `DotNetEnv` / `EnvFile.Load` / `AddEnvironmentVariables`
- No `process.env` / `import.meta.env`
- Sole data path: `useGroups()` then render a table

Empty PASS is the measured result **for this file**, after a full read. It is **not** a claim that `apps/api/Program.cs` loads `.env` before `CreateBuilder`.

---

## 3. Evidence quotes

| Claim | Quote / measurement |
|---|---|
| Display-only fetch | `const { data = [], isLoading } = useGroups();` |
| Plan map is a label | `Plan mappings are labels only — they do not filter ingestion.` |
| Rendered fields only | `{g.broker}` `{g.group}` `{g.accounts}` `{g.enabledForAnalysis ? 'yes' : 'no'}` `{g.planMapping ?? '—'}` |
| Loading is UI only | `if (isLoading) return <p className="text-gray-400">Loading groups…</p>;` |
| No host bootstrap in file | `grep` CreateBuilder / WebApplication / dotenv / IConfiguration / `.env` = **0** |
| No env accessors in file | No `process.env`, `import.meta.env`, `EnvFile`, or `AddEnvironmentVariables` |

---

## 4. No-loss implication

**None on this path.** A missing `.env` load before `WebApplication.CreateBuilder` cannot originate in `GroupsPage.tsx` because the page never starts a .NET host, never reads secrets, and never sends orders / kill-switch / FIX / MT5 manager calls. Worst case is an empty or stale groups table in the dashboard. The page itself states plan mappings do not filter ingestion, so a wrong `planMapping` label here cannot silently drop groups from the collector. Capital / no-loss controls are not reachable from this component.

---

## 5. What this PASS is not

- Not a PASS on `apps/api/Program.cs` dotenv-before-CreateBuilder (not this slot).
- Not a claim that live MT5 group discovery works (page only renders whatever `useGroups()` returns).
- Not a claim that `planMapping` / `enabledForAnalysis` exist as ingestion filters (page copy says they do not).
- Empty PASS is allowed only because the assigned file was fully read (34/34 lines); the angle is absent by construction, not by skipped review.
