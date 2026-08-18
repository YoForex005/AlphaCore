# W500_SLICE_20

- **slot:** 20
- **file:** `D:/Prop/apps/web/src/pages/TradersPage.tsx`
- **angle:** dummy or seeded data still reachable on the live path
- **read:** full file (42 lines) via `read_file`; grep on this file for `dummy|seed|mock|fake|sample|placeholder|hardcoded|DEMO|test.?data|fixture|lorem|john.?doe|Jane|example\.com` returned **no matches**
- **live route:** `/traders` via `D:/Prop/apps/web/src/App.tsx` `path="traders"`
- **one-hop data path:** `useTraders({})` → `GET /api/traders` (no client-side fixture)
- **verdict:** PASS

## Evidence quotes

`TradersPage` is a 42-line leaderboard. It loads via `useTraders({})`, defaults undefined query data to an empty array (not a seeded roster), and maps API fields. There is no inline trader list, no demo login, no fixture import.

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
        ...
        <tbody>
          {data.map((t: any) => (
            <tr key={`${t.broker}-${t.login}`} className="border-b border-gray-800 text-gray-200">
              <td className="py-2">{t.broker}</td>
              <td><Link className="text-blue-300" to={`/traders/${t.broker}/${t.login}`}>{t.login}</Link></td>
              ...
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
```

Live-path wiring (this page is the production `/traders` element, not a story/demo stub):

```27:28:D:/Prop/apps/web/src/App.tsx
        <Route path="traders" element={<TradersPage />} />
        <Route path="traders/:brokerId/:login" element={<TraderDetailPage />} />
```

Hook used by the page is a real HTTP GET. No mock adapter, no `initialData` seed, no fallback rows:

```16:20:D:/Prop/apps/web/src/api/hooks.ts
export function useTraders(filters: { broker?: string; state?: string }) {
  return useQuery({
    queryKey: ['traders', filters],
    queryFn: () => client.get('/api/traders', { params: filters }).then(r => r.data),
  });
}
```

Axios client has no interceptor that injects dummy payloads:

```1:9:D:/Prop/apps/web/src/api/client.ts
import axios from 'axios';

const client = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000',
  timeout: 15000,
  headers: { 'Content-Type': 'application/json' },
});

export default client;
```

Grep of `D:/Prop/apps/web/src` for dummy/seed/mock on the traders live path found seed *copy* only on other pages (`AuditPage`, `ShadowPortfolioPage`), not in `TradersPage.tsx`. File-local grep: **0 hits**.

This file does not contain:

- hardcoded trader arrays / sample logins / fake P&L
- `msw` / `faker` / `mockData` / `SEED` imports
- `placeholderData` / `initialData` demo rows
- a branch that swaps in fixtures when the API is empty or errors
- names, emails, or broker codes invented in the UI

`data = []` is an empty-array default when the query has no `data` yet (or after an error with no cached rows). That is **not** seeded leaderboard content; an empty table cannot impersonate live XAU traders.

Downstream `GET /api/traders` is `IDashboardQueries.GetTradersAsync` over EF (`TraderScores` / `Brokers` / `Mt5Accounts` / `ReconstructedTrades`). That is out of this slice’s file; the page itself does not substitute dummy rows if those tables are empty.

## No-loss implication

This live page cannot mint synthetic traders, scores, or P&L that an operator might copy. Worst case on this file is an empty table or whatever the API already persisted. Slot 20 therefore has **no dummy/seeded capital-decision path** in the assigned file.

Empty-PASS justification: the assigned file was fully read (42/42 lines); the angle (dummy or seeded data on the live path) is absent by construction, not by skipped review.
