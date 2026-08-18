# W500_SLICE_19 — GroupsPage vs dotenv-before-CreateBuilder

| Field | Value |
|---|---|
| Slot | 19 |
| File | `D:\Prop\apps\web\src\pages\GroupsPage.tsx` |
| Angle | env file not loaded before `WebApplication.CreateBuilder` |
| Date | 2026-08-18 |
| Method | `read_file` of the assigned file (full 34 lines) + `grep` on that file and `apps/web` for `CreateBuilder` / dotenv / `process.env` / `import.meta` |
| Product source modified | **No** |
| Verdict | **PASS** (empty PASS — defect class does not apply to this file) |

---

## 1. What was read

`GroupsPage.tsx` is a 34-line React function component. Entire file:

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

`grep` on this file for `env|CreateBuilder|WebApplication|dotenv|process\.env|import\.meta` → **0 hits**.

---

## 2. Angle check

The assigned defect is a **.NET host bootstrap** failure: a dotenv / `.env` file must be applied to the process **before** `WebApplication.CreateBuilder(args)` so that `Configuration` sees `MT5_*` / FIX / connection-string keys.

That pattern exists only in ASP.NET hosts. The product host that *does* call CreateBuilder is `D:\Prop\apps\api\Program.cs` line 7 (`var builder = WebApplication.CreateBuilder(args);`) — **out of scope** for this slot. `GroupsPage.tsx` is Vite/React UI:

- No `Microsoft.AspNetCore` types
- No `WebApplication` / `CreateBuilder`
- No `DotNetEnv` / `AddEnvironmentVariables` / dotenv load
- No `process.env` / `import.meta.env` (the Vite API base URL lives in `apps/web/src/api/client.ts` and `signalr.ts`, not here)
- Sole data path: `useGroups()` → `GET /api/groups` (`hooks.ts` line 12–14)

Empty PASS is therefore the measured result for **this file**, not a claim that the API host loads `.env` before CreateBuilder.

---

## 3. Evidence quotes

| Claim | Quote / measurement |
|---|---|
| Display-only fetch | `const { data = [], isLoading } = useGroups();` |
| Plan map is a label | `Plan mappings are labels only — they do not filter ingestion.` |
| Rendered fields only | `{g.broker}` `{g.group}` `{g.accounts}` `{g.enabledForAnalysis ? 'yes' : 'no'}` `{g.planMapping ?? '—'}` |
| No host bootstrap in file | `grep` CreateBuilder / dotenv / env accessors = **0** |
| Routed as UI only | `App.tsx`: `<Route path="groups" element={<GroupsPage />} />` |

---

## 4. No-loss implication

**None on this path.** A missing `.env` load before `CreateBuilder` cannot originate in `GroupsPage.tsx` because the page never starts a .NET host, never reads secrets, and never sends orders / kill-switch / FIX / MT5 manager calls. Worst case is an empty or stale groups table in the dashboard. The page itself states plan mappings do not filter ingestion, so a wrong `planMapping` label here cannot silently drop groups from the collector. Capital / no-loss controls are not reachable from this component.

---

## 5. What this PASS is not

- Not a PASS on `apps/api/Program.cs` dotenv-before-CreateBuilder (not this slot).
- Not a claim that live MT5 group discovery works (page only renders whatever `/api/groups` returns).
- Not a claim that `planMapping` / `enabledForAnalysis` exist as ingestion filters (page copy says they do not).
