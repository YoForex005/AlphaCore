import { useParams } from 'react-router-dom';
import { useTraderDetail } from '../api/hooks';

export default function TraderDetailPage() {
  const { brokerId = '', login = '' } = useParams();
  const { data, isLoading } = useTraderDetail(brokerId, login);
  if (isLoading) return <p className="text-gray-400">Loading trader…</p>;
  if (!data) return <p className="text-gray-400">Trader not found.</p>;
  const h = data.header ?? data;
  const trades = data.trades ?? [];
  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-semibold text-white">{h.broker} / {h.login}</h1>
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3 text-sm">
        <Info k="State" v={h.state} />
        <Info k="Completed XAU" v={h.completedXauTrades} />
        <Info k="Early score" v={Number(h.earlyScore).toFixed(2)} />
        <Info k="Risk score" v={Number(h.riskScore).toFixed(2)} />
        <Info k="Net P&L" v={Number(h.netSourcePnl).toFixed(2)} />
        <Info k="Martingale" v={h.martingale ? 'yes' : 'no'} />
        <Info k="Averaging down" v={h.averagingDown ? 'yes' : 'no'} />
        <Info k="ML probability" v={h.mlProbability ?? 'not trained'} />
      </div>
      <table className="w-full text-sm">
        <thead className="text-gray-400 border-b border-gray-800">
          <tr>
            <th className="py-2 text-left">Pos</th>
            <th className="text-left">Symbol</th>
            <th className="text-left">Net</th>
            <th className="text-left">First 3</th>
          </tr>
        </thead>
        <tbody>
          {trades.map((t: any) => (
            <tr key={t.positionId} className="border-b border-gray-800 text-gray-200">
              <td className="py-2">{t.positionId}</td>
              <td>{t.canonicalSymbol}</td>
              <td>{Number(t.netRealizedPnl).toFixed(2)}</td>
              <td>{t.isFirstThree ? 'yes' : ''}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <p className="text-xs text-gray-500">First 3 completed XAUUSD trades unlock EARLY_SCORE / SHADOW only. Live promotion is not automatic.</p>
    </div>
  );
}

function Info({ k, v }: { k: string; v: any }) {
  return (
    <div className="bg-gray-800 border border-gray-700 rounded p-3">
      <div className="text-gray-400 text-xs uppercase">{k}</div>
      <div className="text-gray-100 mt-1">{String(v)}</div>
    </div>
  );
}
