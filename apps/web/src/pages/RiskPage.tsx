import { useRiskStatus } from '../api/hooks';
import MetricCard from '../components/MetricCard';

export default function RiskPage() {
  const { data } = useRiskStatus();
  if (!data) return <p className="text-gray-400">Loading risk…</p>;
  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-semibold text-white">Risk</h1>
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <MetricCard label="Kill switch" value={data.killSwitch} />
        <MetricCard label="Real copy" value={data.realCopyEnabled ? 'ON' : 'OFF'} color="text-amber-300" />
        <MetricCard label="Daily P&L" value={Number(data.dailyPnl).toFixed(2)} />
        <MetricCard label="XAU net" value={Number(data.xauNet).toFixed(2)} />
      </div>
      <div>
        <h2 className="text-sm text-gray-400 mb-2">Recent rejects</h2>
        <ul className="text-sm text-gray-200 list-disc pl-5">
          {(data.recentRejectReasons ?? []).map((r: string, i: number) => <li key={i}>{r}</li>)}
          {(data.recentRejectReasons ?? []).length === 0 && <li className="text-gray-500">None</li>}
        </ul>
      </div>
    </div>
  );
}
