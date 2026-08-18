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
