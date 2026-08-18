# W500_SLICE_45 — TradersPage vs net8.0-windows x64 MetaQuotes DLL load

| Field | Value |
|---|---|
| Slot | 45 |
| File | `D:\Prop\apps\web\src\pages\TradersPage.tsx` |
| Angle | API not `net8.0-windows` x64 so MetaQuotes DLL cannot load |
| Date | 2026-08-18 |
| Method | `read_file` of the assigned file (full 42 lines) + `grep` on that file and `apps/web` for `net8.0-windows` / MetaQuotes / x64 / `DllImport` / `.dll` |
| Product source modified | **No** |
| Verdict | **PASS** (empty PASS — defect class does not apply to this file) |

---

## 1. What was read

`TradersPage.tsx` is a 42-line React function component. Entire file:

```1:42:D:/Prop/apps/web/src/pages/TradersPage.tsx
import { Link } from 'react-router-dom';
import { useTraders } from '../api/hooks';

export default function TradersPage() {
  const { data = [], isLoading } = useTraders({});
  if (isLoading) return <p className="text-gray-400">Loading traders…</p>;
  return (
    <div>
      <h1 className="text-2xl font-semibold text-white mb-4">Trader leaderboard</h1>
      <table className="w-full text-sm text-left">
        <thead className="text-gray-400 border-b border-gray-800">
          <tr>
            <th className="py-2">Broker</th>
            <th>Login</th>
            <th>Group</th>
            <th>XAU trades</th>
            <th>Net P&L</th>
            <th>Early</th>
            <th>Risk</th>
            <th>Flags</th>
            <th>State</th>
          </tr>
        </thead>
        <tbody>
          {data.map((t: any) => (
            <tr key={`${t.broker}-${t.login}`} className="border-b border-gray-800 text-gray-200">
              <td className="py-2">{t.broker}</td>
              <td><Link className="text-blue-300" to={`/traders/${t.broker}/${t.login}`}>{t.login}</Link></td>
              <td>{t.group}</td>
              <td>{t.completedXauTrades}</td>
              <td>{Number(t.netSourcePnl).toFixed(2)}</td>
              <td>{Number(t.earlyScore).toFixed(1)}</td>
              <td>{Number(t.riskScore).toFixed(1)}</td>
              <td>{[t.martingale && 'MG', t.averagingDown && 'AVG', t.lotEscalation && 'ESC'].filter(Boolean).join(' ') || '—'}</td>
              <td>{t.state}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
```

`grep` on this file for `net8\.0-windows|MetaQuotes|x64|DllImport|\.dll` → **0 hits**.

`grep` on `D:/Prop/apps/web` for `net8\.0-windows|MetaQuotes` → **0 hits**.

---

## 2. Angle check

The assigned defect is a **.NET RID / TargetFramework** failure: MetaQuotes Manager API native DLLs (`MetaQuotes.MT5ManagerAPI64.dll` and peers) load only when the host process is **Windows x64** and the project is **`net8.0-windows`**. A `net8.0` (non-windows) or AnyCPU/x86 API host cannot `LoadLibrary` those DLLs; Manager `Connect` never happens.

That pattern exists only in C# csproj + P/Invoke / C++/CLI hosts (`apps/api`, `src/Mt5`, workers). `TradersPage.tsx` is Vite/React browser UI:

- No `.csproj` / `TargetFramework` / `RuntimeIdentifier`
- No `net8.0-windows` (or any TFM)
- No `DllImport`, `NativeLibrary`, `LoadLibrary`, `[DllImport]`
- No MetaQuotes types, no `CIMTManagerAPI`, no `MTRetCode`
- No Windows process; the page runs as JS in the browser
- Sole data path: `useTraders({})` → HTTP dashboard query (display only)

Empty PASS is therefore the measured result for **this file**, not a claim that the API host is (or is not) `net8.0-windows` x64.

---

## 3. Evidence quotes

| Claim | Quote / measurement |
|---|---|
| Display-only fetch | `const { data = [], isLoading } = useTraders({});` |
| Loading is UI copy | `if (isLoading) return <p className="text-gray-400">Loading traders…</p>;` |
| Title is leaderboard | `<h1 ...>Trader leaderboard</h1>` |
| Rendered fields only | `{t.broker}` `{t.login}` `{t.group}` `{t.completedXauTrades}` `{t.netSourcePnl}` `{t.earlyScore}` `{t.riskScore}` flags `{t.state}` |
| Flags are labels | `[t.martingale && 'MG', t.averagingDown && 'AVG', t.lotEscalation && 'ESC']` |
| Navigation only | `<Link ... to={\`/traders/${t.broker}/${t.login}\`}>` |
| No native load in file | `grep` `net8.0-windows` / MetaQuotes / x64 / `DllImport` / `.dll` = **0** |
| No MetaQuotes in `apps/web` | `grep` `net8.0-windows` / MetaQuotes under `apps/web` = **0** |

---

## 4. No-loss implication

**None on this path.** A wrong API TFM (`net8.0` instead of `net8.0-windows`) cannot originate in `TradersPage.tsx` because the page is not a .NET process, never loads MetaQuotes DLLs, never calls Manager `Connect`, and never sends orders / kill-switch / FIX. Worst case is an empty or stale trader table in the dashboard (`data = []` while loading or if the HTTP hook returns empty). Displayed `netSourcePnl` / scores / MG-AVG-ESC flags are read-only labels; they do not size, open, or close destination exposure. Capital / no-loss controls are not reachable from this component.

---

## 5. What this PASS is not

- Not a PASS on `apps/api` / `src/Mt5` being `net8.0-windows` x64 (not this slot).
- Not a claim that MetaQuotes DLLs load in any host.
- Not a claim that live MT5 trader census is complete (page only renders whatever `useTraders` returns).
- Not a claim that `netSourcePnl` / scores are computed correctly (display `toFixed` only).

Empty-PASS justification: the assigned file was fully read (42 lines); the angle (API not `net8.0-windows` x64 so MetaQuotes DLL cannot load) is **absent by construction** — this type never hosts the API and never loads a DLL.
