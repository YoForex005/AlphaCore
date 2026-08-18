# W500_SLICE_96

- **slot:** 96
- **file:** `D:/Prop/apps/web/src/pages/OverviewPage.tsx`
- **angle:** dashboard hiding accounts that have no score yet
- **read:** full file (35 lines) via `read_file`; grep on this file for `score|account|hide|filter|no.?score` (2 hits: subtitle “baseline scores”, `data.totalAccounts` tile). Adjacent read (no product edit): `apps/web/src/api/hooks.ts` `useOverview`, `apps/web/src/pages/TradersPage.tsx`, `apps/web/src/types/index.ts`, `src/Infrastructure/Dashboard/EfDashboardQueries.GetOverviewAsync` / `GetTradersAsync`.
- **verdict:** PASS

## Evidence quotes

`OverviewPage` is a 12-card aggregate dashboard. It never maps an account list, never inspects a per-account `score` / `earlyScore`, and never applies a client-side predicate that would drop rows with missing scores.

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

What is **not** in this file (searched; absent):

- `.filter(`, `.find(`, `hide`, `hidden`, `score ===`, `score >`, `earlyScore`, `hasScore`, `unscored`
- any `map` over traders/accounts
- query params that would ask the API for scored-only rows

The only fetch is unparameterized:

```4:6:D:/Prop/apps/web/src/api/hooks.ts
export function useOverview() {
  return useQuery({ queryKey: ['overview'], queryFn: () => client.get('/api/overview').then(r => r.data), refetchInterval: 4000 });
}
```

Headline account count is **not** derived from `TraderScores`. Backend `GetOverviewAsync` counts every `Mt5Accounts` row, then separately counts score-table buckets (XAU / ≥3 / Watch / Shadow / Live / Risk). Unscored logins still increment `totalAccounts`; they simply do not increment score-state tiles — that is a different metric, not a hide:

```21:40:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
    public async Task<OverviewDto> GetOverviewAsync(CancellationToken ct)
    {
        var accounts = await _db.Mt5Accounts.CountAsync(ct);
        var brokers = await _db.Brokers.CountAsync(b => b.Enabled, ct);
        var scores = await _db.TraderScores.ToListAsync(ct);
        var xauTraders = scores.Count(s => s.CompletedXauTrades > 0);
        var three = scores.Count(s => s.CompletedXauTrades >= 3);
        // ...
            accounts,
            brokers,
            xauTraders,
            three,
            scores.Count(s => s.CurrentState == TraderState.WATCH),
            // ...
```

Adjacent leaderboard (not this slice’s file) also does **not** drop unscored accounts: `GetTradersAsync` iterates `Mt5Accounts` and defaults missing scores to `0` / `INSUFFICIENT_DATA`. `TradersPage` then `data.map`s every returned row.

This page cannot hide an account because it never lists one. `if (!data) return null` is empty payload, not a score gate.

## No-loss implication

No capital path exists on this page: it only paints `/api/overview` aggregates. It cannot omit an unscored source login from the **MT5 accounts** tile (that number is `Mt5Accounts.Count`, not `TraderScores.Count`). Operators still see the full ingested account census even when scoring has not produced a row. Score-state tiles (Watch / Shadow / Live candidates) correctly exclude logins that have no `TraderScores` row — those are pipeline-stage counts, not a stealth filter that would let an unscored account copy or disappear from ops. Footer states live FIX send is off and Trade #3 never auto-promotes to LIVE. Worst case on this file: loading/error/`null` — no order, no hide-to-copy, no silent promotion.

Empty-PASS justification: assigned file was fully read (35/35 lines). Hide-by-missing-score is absent by construction (no roster, no score predicate), not by skipped review.
