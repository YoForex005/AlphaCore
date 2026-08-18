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
