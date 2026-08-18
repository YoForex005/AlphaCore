# W500_SLICE_94 — GroupsPage vs Achiever HTTP proxy / MT_RET_AUTH_MANAGER_IPBLOCK 1012

| Field | Value |
|---|---|
| Slot | 94 |
| File | `D:/Prop/apps/web/src/pages/GroupsPage.tsx` |
| Angle | Achiever HTTP proxy / `MT_RET_AUTH_MANAGER_IPBLOCK` 1012 |
| Date | 2026-08-18 |
| Method | `read_file` of the assigned file **twice** (file changed mid-pass; second read is authoritative, 38 lines) + `grep` on this file and `apps/web` for `1012` / `IPBLOCK` / `proxy` / `ACHIEVER_PROXY` / `MT_RET`; adjacent read of `hooks.ts`, `/api/groups`, `EfDashboardQueries.GetGroupsAsync`, `NativeMt5BrokerConnector` `ApplyProxy`/`Describe`, `LiveMt5Registration`, `LiveRuntimeStatus.Snapshot` |
| Product source modified | **No** |
| Secrets printed | **None.** No manager passwords, no proxy username/password, no FIX passwords. Proxy keys named only. |
| Verdict | **PASS** (empty PASS on this file’s 1012/proxy contract — file was fully read) |

---

## 1. What was read (assigned file, full)

Second `read_file` of `D:/Prop/apps/web/src/pages/GroupsPage.tsx` (38 lines). Entire file:

```1:38:D:/Prop/apps/web/src/pages/GroupsPage.tsx
import { useGroups, useIngestStatus } from '../api/hooks';

export default function GroupsPage() {
  const { data = [], isLoading } = useGroups();
  const ingest = useIngestStatus();
  if (isLoading) return <p className="text-gray-400">Loading groups…</p>;
  return (
    <div>
      <h1 className="text-2xl font-semibold text-white mb-2">MT5 Groups</h1>
      <p className="text-sm text-gray-400 mb-4">Every group visible to the Achiever and Starwave managers. Count: {data.length}.</p>
      {ingest.data?.brokers && (
        <p className="text-xs text-gray-500 mb-3">{JSON.stringify(ingest.data.brokers)}</p>
      )}
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

`grep` on this file for `1012|IPBLOCK|proxy|Proxy|ACHIEVER_PROXY|MT_RET|Connect|ProxySet` → **0 hits** (only incidental `border-b` / class names; no proxy or retcode tokens).  
`grep` on `apps/web` for those angle tokens → **0** in product TS/TSX (only `package-lock.json` npm `https-proxy-agent` / `proxy-from-env`, unrelated to Achiever Manager).

This is **not** an unread-file empty PASS. The assigned file was read in full.

---

## 2. Angle check (does this file own HTTP proxy / 1012?)

| Question | Measured answer |
|---|---|
| Does `GroupsPage.tsx` call Manager `Connect`? | **No.** |
| Does it call `ProxySet` / set `PROXY_HTTP`? | **No.** |
| Does it read `ACHIEVER_PROXY_ENABLED` / host / port / user / password? | **No.** |
| Does it mention or map `1012` / `MT_RET_AUTH_MANAGER_IPBLOCK`? | **No.** |
| Can it skip the whitelist hop and still open a live Achiever session? | **No** — it never opens a session. |
| Can it send orders / kill-switch / FIX / size? | **No** — render only. |
| Data path for the table | `useGroups()` → `GET /api/groups` (`hooks.ts` L12–14) |
| Data path for the JSON dump | `useIngestStatus()` → `GET /api/ingest/status` (`hooks.ts` L16–17) |
| Does `/api/groups` hit live Manager? | **No.** `Program.cs` L94: `q.GetGroupsAsync` → `EfDashboardQueries` reads `Mt5Groups` from EF. |
| Where 1012 actually lives | `NativeMt5BrokerConnector.Describe` (`1012 => "manager IP blocked — Achiever requires the whitelist HTTP proxy"`) after `ApplyProxy` + `Connect` |

The assigned defect class is a **Manager authentication** refusal: Achiever sees a TCP source that is not allow-listed (`81.29.145.69`) and returns `MT_RET_AUTH_MANAGER_IPBLOCK = 1012`. Recovery is HTTP `ProxySet` **before** `Connect` (`ApplyProxy` when `ProxyEnabled` and host are set; wired from `ACHIEVER_PROXY_*` in `LiveMt5Registration`). That contract is entirely outside this React file.

Empty PASS is therefore the measured result for **this file**, not a claim that Achiever local connect works without the HTTP hop.

---

## 3. Evidence quotes

### 3.1 This page is a catalog viewer

Hook used by the table (`apps/web/src/api/hooks.ts`):

```12:14:D:/Prop/apps/web/src/api/hooks.ts
export function useGroups() {
  return useQuery({ queryKey: ['groups'], queryFn: () => client.get('/api/groups').then(r => r.data), refetchInterval: 4000 });
}
```

API is EF, not Manager:

```94:94:D:/Prop/apps/api/Program.cs
app.MapGet("/api/groups", (IDashboardQueries q, CancellationToken ct) => q.GetGroupsAsync(ct));
```

```68:80:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
    public async Task<IReadOnlyList<GroupRowDto>> GetGroupsAsync(CancellationToken ct)
    {
        var groups = await _db.Mt5Groups.ToListAsync(ct);
        var brokers = await _db.Brokers.ToDictionaryAsync(b => b.Id, ct);
        var rows = new List<GroupRowDto>();
        foreach (var g in groups)
        {
            var code = brokers.TryGetValue(g.BrokerId, out var b) ? b.Code : g.BrokerId.ToString();
            var accounts = await _db.Mt5Accounts.CountAsync(a => a.BrokerId == g.BrokerId && a.GroupName == g.Name, ct);
            rows.Add(new GroupRowDto(code, g.Name, accounts, g.EnabledForAnalysis, g.PlanMapping, g.LastDiscoveredAt, g.LastSyncedAt));
        }

        return rows;
    }
```

Rendered columns only: `g.broker`, `g.group`, `g.accounts`, `g.enabledForAnalysis`, `g.planMapping`. DTO fields `LastDiscovered` / `LastSynced` are **not** shown. No write, no filter POST, no group enable toggle.

### 3.2 1012 / HTTP proxy (adjacent, not this file)

`Connect` applies proxy then fails closed; 1012 is named in the mapper:

```115:129:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private void ApplyProxy()
    {
        if (_manager is null || !_opt.ProxyEnabled || string.IsNullOrWhiteSpace(_opt.ProxyHost))
            return;
        var proxy = new MTProxyInfo
        {
            enable = 1,
            type = MTProxyInfo.Type.PROXY_HTTP,
            address = $"{_opt.ProxyHost}:{_opt.ProxyPort}",
            auth = string.IsNullOrEmpty(_opt.ProxyUser) ? "" : $"{_opt.ProxyUser}:{_opt.ProxyPassword}"
        };
        var set = _manager.ProxySet(proxy);
        if (set != MTRetCode.MT_RET_OK)
            throw new InvalidOperationException(Describe(set, $"{BrokerCode} ProxySet"));
```

```442:454:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private static string Describe(MTRetCode code, string op)
    {
        var hint = (int)code switch
        {
            7 => "network/timeout — check proxy, firewall, server",
            1012 => "manager IP blocked — Achiever requires the whitelist HTTP proxy",
            // ...
        };
        return $"{op} failed: {(int)code} {code} ({hint})";
    }
```

Official constant (vendor, not this page): `MT_RET_AUTH_MANAGER_IPBLOCK = 1012` — “IP address unallowed for manager.”  
Live ingest does **not** substitute dummy groups on that throw (`LiveIngestHostedService` catch: `Connected = false`, `Phase = "failed"`, log “No dummy data will be substituted.”).

### 3.3 Honesty residual (not a capital FAIL)

Caption L10 asserts:

> Every group visible to the Achiever and Starwave managers.

That sentence is **not** true of the fetch: rows are whatever is already in `Mt5Groups` (seed and/or last successful upsert). If this workstation’s egress is not the Achiever allow-list and `ACHIEVER_PROXY_ENABLED` is not applied, Manager `Connect` is expected **1012** (R012). The table would still list persisted names. The page does **not** gate the table on `ingest.data.brokers[].Connected` / `LastError` / `Phase`.

It does dump `ingest.data.brokers` as raw JSON (`LiveRuntimeStatus.Snapshot` includes `Connected`, `LastError`, `Phase`). A 1012 failure **can** appear in that dump if live ingest ran and threw `Describe(..., 1012)`. That is an unformatted operator breadcrumb, not a ProxySet implementation and not a silent “connected” lie in code.

Classification: dashboard overclaim / missing 1012 banner. **Out of the 1012 fail-closed contract this slot judges.** Does not flip the verdict to FAIL.

---

## 4. No-loss implication

**None on this path.** `GroupsPage.tsx` cannot apply or omit the Achiever HTTP proxy, cannot call `Connect`, and cannot receive `MT_RET_AUTH_MANAGER_IPBLOCK` itself.

`1012` is an **authentication** retcode on the Manager hop: “IP address unallowed for manager.” It rejects the session **before** pump / `GroupRequestArray` / deal or position APIs. Equity cannot move on a connection that never authenticated. That fail-closed lives in `NativeMt5BrokerConnector.ConnectCore`, not here.

This page:

- does not send `NewOrderSingle` / dealer / MT5 order mutators
- does not read or log proxy auth
- does not use `planMapping` / `enabledForAnalysis` as a fetch filter (table cells only; collector still enumerates all connector groups — D68)
- worst case if Achiever is 1012-blocked: empty or **stale EF catalog** plus optional ingest JSON with `Connected: false` / `LastError` containing the 1012 hint

Stale group labels cannot open, close, or size destination positions. Capital / no-loss controls are not reachable from this component.

---

## 5. What this PASS is not

- Not a PASS that local Achiever connect works without HTTP proxy (R012: this LAN egress ≠ `81.29.145.69` → 1012 when proxy is off).
- Not a PASS that live Achiever / Starwave Manager sessions are proven (C42; this page never connects).
- Not a PASS that the L10 caption is accurate (it overclaims manager visibility of EF rows).
- Not a review of `NativeMt5BrokerConnector` / `LiveMt5Registration` / YoPips `IS_MT5_PROXY_ENABLED` toggle (other slots).

Empty-PASS justification: assigned file fully read (38 lines); angle tokens absent by construction; 1012/proxy contract is not implemented here and cannot be violated here.
