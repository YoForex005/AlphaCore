# W500_SLICE_46

- **slot:** 46
- **file:** `D:/Prop/apps/web/src/pages/OverviewPage.tsx`
- **angle:** dashboard hiding accounts that have no score yet
- **read:** full file (35 lines) via `read_file`; grep on this file for `score|account|hide|filter|hidden` (case-insensitive) returned 2 hits — subtitle copy “baseline scores” and `data.totalAccounts` only; no `.filter`, no score predicate, no account roster
- **verdict:** **PASS**

## Evidence quotes

`OverviewPage` is a metrics-only dashboard. It loads `useOverview()` (raw `GET /api/overview`, no query params) and paints twelve `MetricCard` tiles plus a real-copy banner. There is no account/trader table, no per-login row, and no client-side predicate that drops accounts missing a score.

```1:35:D:/Prop/apps/web/src/pages/OverviewPage.tsx
import MetricCard from '../components/MetricCard';
import { useOverview } from '../api/hooks';

export default function OverviewPage() {
  const { data, isLoading, error } = useOverview();
  if (isLoading) return <p className="text-gray-400">Loading overview…</p>;
  if (error) return <p className="text-red-400">API unavailable. Start the ASP.NET API on port 5000.</p>;
  if (!data) return null;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold text-white">Overview</h1>
        <p className="text-sm text-gray-400">First useful version: ingestion, reconstruction, baseline scores, shadow-ready. Live FIX send is off.</p>
      </div>
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <MetricCard label="MT5 accounts" value={data.totalAccounts} />
        <MetricCard label="Brokers" value={data.connectedBrokers} />
        <MetricCard label="XAU traders" value={data.xauTraders} />
        <MetricCard label="≥ 3 trades" value={data.tradersWithThreeTrades} />
        <MetricCard label="Watch" value={data.watch} />
        <MetricCard label="Shadow" value={data.shadow} color="text-blue-300" />
        <MetricCard label="Live candidates" value={data.liveCandidates} />
        <MetricCard label="Risk blocked" value={data.riskBlocked} color="text-amber-300" />
        <MetricCard label="Shadow P&L" value={Number(data.shadowPnl).toFixed(2)} />
        <MetricCard label="Dest. real P&L" value={Number(data.destinationRealPnl).toFixed(2)} />
        <MetricCard label="MT5 health" value={data.mt5Healthy ? 'OK' : 'DOWN'} color={data.mt5Healthy ? 'text-emerald-300' : 'text-red-400'} />
        <MetricCard label="QUOTE / TRADE" value={`${data.quoteHealthy ? 'Q' : '-'} / ${data.tradeHealthy ? 'T' : '-'}`} />
      </div>
      <div className="rounded border border-gray-800 bg-gray-950 p-4 text-sm text-gray-300">
        Real copy execution: <strong className="text-amber-300">{data.realCopyEnabled ? 'ON' : 'OFF'}</strong>. Trade #3 never auto-promotes to LIVE.
      </div>
    </div>
  );
}
```

Hook used by this page (no score/filter params):

```4:6:D:/Prop/apps/web/src/api/hooks.ts
export function useOverview() {
  return useQuery({ queryKey: ['overview'], queryFn: () => client.get('/api/overview').then(r => r.data) });
}
```

This file does not contain:

- an account or trader list (`map` over logins / `Mt5Accounts`)
- `.filter(...)` on `score`, `earlyScore`, `riskScore`, or “has score”
- a hide/collapse/visibility gate for unscored rows
- a request param that would exclude accounts without `TraderScore`

`totalAccounts` is rendered as-is. Score is mentioned only in the subtitle string “baseline scores”. State tiles (`Watch`, `Shadow`, `Live candidates`, `Risk blocked`) are opaque counts from the API; this page does not compute them and does not omit `totalAccounts` when those buckets are empty.

Out of slice (not used to fail this file): `/traders` (`TradersPage`) renders whatever `GET /api/traders` returns and also has no client-side “must have score” hide. If unscored logins are invisible on the leaderboard, that is an API/query issue (`GetTradersAsync` / `TraderScores`), not `OverviewPage`.

Empty-PASS justification: the assigned file was fully read (35/35 lines). The angle (this dashboard hiding accounts that have no score yet) is **absent by construction** — there is no account roster and no score hide filter.

## No-loss implication

`OverviewPage` cannot hide an unscored live login from the MT5-accounts census and cannot send, size, or cancel orders. Worst case is an operator reading aggregate tiles (including score-derived state counts that the API computed) while the page itself still shows `totalAccounts` unfiltered. Missing-score accounts are not client-hidden here; no-loss / size-down decisions are not driven by a local “drop if no score” gate. Capital risk from this file is **display-only**.
