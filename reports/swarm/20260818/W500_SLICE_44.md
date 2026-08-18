# W500_SLICE_44 — GroupsPage vs Achiever HTTP proxy / MT_RET_AUTH_MANAGER_IPBLOCK 1012

| Field | Value |
|---|---|
| Slot | 44 |
| File | `D:\Prop\apps\web\src\pages\GroupsPage.tsx` |
| Angle | Achiever HTTP proxy / `MT_RET_AUTH_MANAGER_IPBLOCK` (1012) |
| Date | 2026-08-18 |
| Method | `read_file` of the assigned file (full 34 lines) + `grep` on that file for `1012` / `IPBLOCK` / `proxy` / `Achiever` / `whitelist` / `isError` / `LastDiscovered` |
| Product source modified | **No** |
| Verdict | **PASS** (empty PASS — defect class does not apply to this file; file was actually read) |

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

`grep` on **this file** for `1012|IPBLOCK|IP.?block|proxy|Proxy|Achiever|whitelist|81\.29` → **0 hits**.

`grep` on **this file** for `isError|error|LastDiscovered|LastSynced|1012|proxy` → **0 hits**.

---

## 2. Angle check

The assigned defect is **Achiever Manager connect from a non-allow-listed egress without the HTTP proxy**, which MetaQuotes returns as **`MT_RET_AUTH_MANAGER_IPBLOCK = 1012`**. That path lives in the native/C# Manager connector, not in this Vite page.

Measured location of 1012 / HTTP proxy (adjacent, **not this file**):

- `NativeMt5BrokerConnector.Describe` maps `1012 => "manager IP blocked — Achiever requires the whitelist HTTP proxy"`.
- `ApplyProxy` runs only when `ProxyEnabled` and `ProxyHost` are set; type `PROXY_HTTP`; address `host:port`. Credentials are not quoted here.
- `LiveMt5Registration` wires Achiever `ACHIEVER_PROXY_*` env; StarwaveFX `ProxyEnabled = false`.
- R012 (ops, not this slot): local desktop egress is not `81.29.145.69`; direct Achiever connect historically returns 1012 when proxy is disabled.

`GroupsPage.tsx` cannot cause, retry, or recover from 1012:

- No `Connect` / `ProxySet` / `CIMTManagerAPI`
- No `ACHIEVER_PROXY_*` / `ProxyEnabled` / env reads
- No Manager login, server, or egress IP
- No retry loop that would hammer Achiever without a proxy
- Sole data path: `useGroups()` → `GET /api/groups` (`hooks.ts` L12–14) → `EfDashboardQueries.GetGroupsAsync` (EF `Mt5Groups` table), **not** `NativeMt5BrokerConnector.GetGroupsCore`

The subtitle **"Discovered dynamically"** is **static copy**. It is not a live `GroupRequestArray` / `GroupTotal` call. DTO fields `LastDiscovered` / `LastSynced` exist on `GroupRowDto` (`DashboardModels.cs` L34–41) but this page never renders them. That is an honesty/UX gap on a different axis (operator cannot see stale catalog after a 1012), **not** a proxy-config or 1012-handling defect **in this file**.

Empty PASS is therefore the measured result for **this file**, not a claim that Achiever connect works, that the HTTP proxy is enabled, or that live group discovery is proven.

---

## 3. Evidence quotes

| Claim | Quote / measurement |
|---|---|
| Display-only fetch | `const { data = [], isLoading } = useGroups();` |
| Loading is the only branch | `if (isLoading) return <p className="text-gray-400">Loading groups…</p>;` — `isError` unused |
| Static discovery copy | `Discovered dynamically. Plan mappings are labels only — they do not filter ingestion.` |
| Rendered fields only | `{g.broker}` `{g.group}` `{g.accounts}` `{g.enabledForAnalysis ? 'yes' : 'no'}` `{g.planMapping ?? '—'}` |
| No 1012 / proxy / Achiever tokens in file | `grep` 1012 / IPBLOCK / proxy / Achiever / whitelist = **0** |
| Hook is HTTP GET, not Manager | `hooks.ts`: `client.get('/api/groups')` |
| API is dashboard EF, not Connect | `Program.cs`: `app.MapGet("/api/groups", … q.GetGroupsAsync(ct))`; `GetGroupsAsync` reads `_db.Mt5Groups` |
| 1012 mapping is elsewhere | `NativeMt5BrokerConnector.cs`: `1012 => "manager IP blocked — Achiever requires the whitelist HTTP proxy"` |
| Routed as UI only | `App.tsx`: `<Route path="groups" element={<GroupsPage />} />` |

---

## 4. No-loss implication

**None on this path.** `GroupsPage.tsx` never opens an Achiever Manager session, never sets or omits the HTTP proxy, and never sends FIX / copy / kill-switch / NewOrderSingle. A 1012 IP-block cannot originate here. The page cannot disable `ACHIEVER_PROXY_ENABLED` or bypass `ApplyProxy`.

Worst case on this component: empty table (`data = []`) or a **stale** `Mt5Groups` paint while Achiever is actually 1012-blocked. The page copy states plan mappings **do not filter ingestion**, so a wrong `planMapping` / `enabledForAnalysis` label here cannot silently drop or include groups on the collector. Ingestion group enumeration is `DealIngestionService` → `connector.GetGroupsAsync` after `ConnectAsync` (fails closed when not connected). Capital / no-loss controls are not reachable from this component.

---

## 5. What this PASS is not

- Not a PASS on Achiever live connect, `ACHIEVER_PROXY_ENABLED`, or `ProxySet` (`NativeMt5BrokerConnector` / `LiveMt5Registration` — not this slot).
- Not a PASS that 1012 is surfaced to the operator (this page has no error / LastError / LastDiscovered column).
- Not a claim that `/api/groups` rows were discovered from a live Manager (EF catalog / seed can still render).
- Not a claim that live MT5 group discovery works.
- Passwords, proxy `auth=user:pass`, and FIX passwords were not printed.

---

## 6. Slot contract

File read in full (34 lines). Angle inapplicable to this UI surface. Empty PASS allowed only after that read — **satisfied**.
