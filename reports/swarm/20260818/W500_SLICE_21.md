# W500_SLICE_21

- **slot:** 21
- **file:** `D:/Prop/apps/web/src/pages/OverviewPage.tsx`
- **angle:** failure to fetch ALL manager groups
- **read:** full file (35 lines) via `read_file`; grep on this file for `group|Group|manager|GetAll|useGroups|/api/groups|GroupTotal|GroupNext` returned **no matches**
- **adjacent (read, not this slice’s contract):** `apps/web/src/api/hooks.ts` `useOverview` / `useGroups`; `apps/api/Program.cs` `GET /api/overview` vs `GET /api/groups`; `Infrastructure/Dashboard/EfDashboardQueries.GetOverviewAsync`; `pages/GroupsPage.tsx`
- **verdict:** PASS

## Evidence quotes

`OverviewPage` is a read-only metric grid. It imports only `MetricCard` and `useOverview`. It never calls `useGroups`, never hits `/api/groups`, and never enumerates Manager API groups (`GroupTotal` / `GroupNext` / `GetAllGroups`).

```1:8:D:/Prop/apps/web/src/pages/OverviewPage.tsx
import MetricCard from '../components/MetricCard';
import { useOverview } from '../api/hooks';

export default function OverviewPage() {
  const { data, isLoading, error } = useOverview();
  if (isLoading) return <p className="text-gray-400">Loading overview…</p>;
  if (error) return <p className="text-red-400">API unavailable. Start the ASP.NET API on port 5000.</p>;
  if (!data) return null;
```

The painted fields are account/broker/score/PnL/health tiles plus a real-copy banner. None are a group list.

```16:32:D:/Prop/apps/web/src/pages/OverviewPage.tsx
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
```

Wire used by this page (not a group enumerator):

```4:5:D:/Prop/apps/web/src/api/hooks.ts
export function useOverview() {
  return useQuery({ queryKey: ['overview'], queryFn: () => client.get('/api/overview').then(r => r.data) });
```

Manager-group listing lives on a **different** page and hook (`GroupsPage` + `useGroups` → `GET /api/groups`). Ingestion enumerates groups in `DealIngestionService` via `connector.GetGroupsAsync`. Those paths are out of this slice’s file.

This file does not contain:

- `useGroups` / `GET /api/groups`
- `GroupTotal` / `GroupNext` / `GroupRequest` / `GetAllGroups`
- plan-map intersection (`getMt5Group`, `MT5_GROUP_*`)
- pagination / `Take(` / first-N group cutoff
- any POST/PUT/DELETE or order-send

There is therefore no truncated or plan-filtered manager-group fetch on Overview. The page cannot “fail to fetch ALL groups” because it does not fetch groups.

## No-loss implication

Omitting group discovery on Overview cannot drop accounts from ingestion, cannot shrink the manager-visible universe, and cannot place, size, or cancel destination orders. Worst case is a dashboard that does not list groups (operators use `/groups` for that). Slot 21 therefore has **no live capital-loss path** and **no missed-group execution path** in the assigned file.

Empty-PASS justification: the assigned file was fully read (35/35 lines); the angle (failure to fetch ALL manager groups) is absent by construction, not by skipped review.
