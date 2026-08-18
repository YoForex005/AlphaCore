# W500_SLICE_70 — TradersPage vs dummy/seeded data on the live path

| Field | Value |
|---|---|
| Slot | 70 |
| File | `D:/Prop/apps/web/src/pages/TradersPage.tsx` |
| Angle | dummy or seeded data still reachable on the live path |
| Date | 2026-08-18 |
| Method | Full `read_file` of the assigned file (42/42 lines). `grep` on that file for dummy/seed/mock/fake/sample/placeholder/hardcoded/fixture/demo (`-i`) → **0**. Cross-read live mount (`App.tsx`), `useTraders` (`hooks.ts`), axios `client.ts`, `GET /api/traders` (`apps/api/Program.cs`), `GetTradersAsync` (`EfDashboardQueries.cs`). `grep` `/api/traders` and `DemoSeeder` on the API host. |
| Product source modified | **No** |
| Verdict | **PASS** |

Empty PASS is allowed only after a full read. The assigned file was fully read (42 physical lines). Dummy/seeded trader rows are **absent from this live UI path by construction**, not by skipped review.

---

## 1. What was read (assigned file, entire module)

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

`grep` on `D:/Prop/apps/web/src/pages/TradersPage.tsx` for `dummy|seed|mock|fake|sample|placeholder|hardcoded|fixture|demo` (`-i`) → **0 matches**.

No local trader arrays. No `10001` / `10002` / `10003` / `99001`. No `FakeMt5` / `DemoSeeder` / `DemoBrokerFactory`. No MSW handlers. No `Math.random` / fixture JSON import.

---

## 2. Live path (this page)

```text
App.tsx  path="traders"  →  <TradersPage />
  useTraders({})
    queryKey ['traders', {}]
    client.get('/api/traders', { params: {} })
      axios baseURL = VITE_API_URL || http://localhost:5000
        Program.cs  MapGet("/api/traders")
          IDashboardQueries.GetTradersAsync(broker: null, state: null)
            EfDashboardQueries: walk Mt5Accounts + TraderScores + completed-trade PnL
```

Hook (live fetch only; no fallback payload):

```16:20:D:/Prop/apps/web/src/api/hooks.ts
export function useTraders(filters: { broker?: string; state?: string }) {
  return useQuery({
    queryKey: ['traders', filters],
    queryFn: () => client.get('/api/traders', { params: filters }).then(r => r.data),
  });
}
```

Client is plain axios. No interceptor, no mock adapter:

```1:9:D:/Prop/apps/web/src/api/client.ts
import axios from 'axios';

const client = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000',
  timeout: 15000,
  headers: { 'Content-Type': 'application/json' },
});

export default client;
```

Route is the live dashboard leaf, not a storybook/demo branch:

```27:27:D:/Prop/apps/web/src/App.tsx
        <Route path="traders" element={<TradersPage />} />
```

The only client-side default is **empty**, not seeded:

- `const { data = [], isLoading } = useTraders({});`
- Loading: `Loading traders…` (copy only)
- Error / undefined `data` after React Query retries: same `[]` → empty `<tbody>`
- That is fail-open to “nobody exists,” **not** a canned four-login book

---

## 3. Downstream host (confirm this page cannot reach DemoSeeder)

Current `apps/api/Program.cs` maps traders to the EF query. It does **not** call `DemoSeeder`:

```95:98:D:/Prop/apps/api/Program.cs
app.MapGet("/api/traders", (IDashboardQueries q, string? broker, string? state, CancellationToken ct) =>
    q.GetTradersAsync(broker, state, ct));
app.MapGet("/api/traders/{broker}/{login:long}", (IDashboardQueries q, string broker, long login, CancellationToken ct) =>
    q.GetTraderDetailAsync(broker, login, ct));
```

Startup seed on this host is catalog-only (`BrokerCatalogSeed.EnsureAsync` after `EnsureCreatedAsync`). `grep` `DemoSeeder|SeedAsync|10001` on `D:/Prop/apps/api/Program.cs` → **0**.

`GetTradersAsync` builds rows from **persisted accounts**, not a hardcoded login list:

```83:118:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
    public async Task<IReadOnlyList<TraderRowDto>> GetTradersAsync(string? broker, string? state, CancellationToken ct)
    {
        var scores = await _db.TraderScores.AsNoTracking().ToListAsync(ct);
        var brokers = await _db.Brokers.AsNoTracking().ToDictionaryAsync(b => b.Id, ct);
        var accounts = await _db.Mt5Accounts.AsNoTracking().ToListAsync(ct);
        // ...
        foreach (var account in accounts)
        {
            // ...
            mapped.Add(new TraderRowDto(
                b.Code,
                account.Login,
                account.GroupName,
                s?.CompletedXauTrades ?? 0,
                pnl,
                s?.EarlyQualityScore ?? 0,
                null,
                s?.RiskScore ?? 0,
                // ...
```

Unscored accounts still appear (zeros / `INSUFFICIENT_DATA`). Those zeros are missing-score defaults, **not** demo tape (`10001` / `10_000` live only in `FakeMt5BrokerConnector`, out of this file).

`/api/ops/resync` now scores `store.ListLoginsAsync` for `ACHIEVER` / `STARWAVEFX`, not `{10001,10002,10003,99001}`.

---

## 4. Evidence quotes (angle matrix)

| Claim | Measurement |
|---|---|
| Page has no dummy/seed tokens | `grep -i dummy\|seed\|mock\|fake\|sample\|placeholder\|hardcoded\|fixture\|demo` on `TradersPage.tsx` = **0** |
| Live data source | `useTraders({})` → `client.get('/api/traders')` |
| Default is empty, not a book | `const { data = [], isLoading } = useTraders({});` |
| Rows are API fields only | `t.broker` `t.login` `t.group` `t.completedXauTrades` `t.netSourcePnl` `t.earlyScore` `t.riskScore` flags `t.state` |
| No canned logins in this file | No `10001` / `10002` / `10003` / `99001` |
| Live route | `App.tsx` `<Route path="traders" element={<TradersPage />} />` |
| API is query, not seeder | `MapGet("/api/traders", … q.GetTradersAsync …)` |
| Host no longer runs DemoSeeder | `Program.cs` grep `DemoSeeder` = **0**; startup is `EnsureCreatedAsync` + `BrokerCatalogSeed.EnsureAsync` |
| `t: any` | Type hole only; does not inject rows |

Sister pages (`AuditPage` “demo seed” copy, `ShadowPortfolioPage` “Demo seed reconstructs…”) are **not** this slot. They do not import into `TradersPage`.

---

## 5. No-loss implication

**None on this path.** `TradersPage` is a read-only table. It does not size, flatten, copy, send FIX `NewOrderSingle`, toggle a kill-switch, or write scores. Dummy Achiever/Starwave demo traders cannot be **created** by this file: it never constructs rows. Worst case inside the assigned file is an empty table (`data = []` on load-error) or painting whatever `GET /api/traders` already stored. Painting a leftover DB row is observability, not a capital path. No-loss controls are not reachable from this component.

Residual (not a FAIL of this file): HTTP 5xx becomes a silent empty `<tbody>` (`isError` unused). That can hide a down API; it still does **not** substitute seeded PnL or open positions.

---

## 6. What this PASS is not

- Not a PASS that `DemoSeeder.cs` / `FakeMt5BrokerConnector` have been deleted (out of slot).
- Not a claim that a previously seeded Postgres/InMemory DB cannot still contain `10001`–`99001` if an older process wrote them — this page would display those store rows if present.
- Not a claim that `/traders` is the architecture §50 / A92 leaderboard (filters, envelope, pagination still absent).
- Not a claim that `Number(null).toFixed(1)` cannot paint a fake `0.0` if the wire later nulls scores — that is a paint-null issue, not a seeded book.
- Empty PASS is valid **only** because `TradersPage.tsx` was fully read (42/42).

**One-line:** Live `/traders` paints `GET /api/traders` or an empty tbody; this file does not ship or fall back to dummy/seeded traders.
