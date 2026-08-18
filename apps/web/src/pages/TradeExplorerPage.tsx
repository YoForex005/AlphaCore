import { useTrades } from '../api/hooks';

export default function TradeExplorerPage() {
  const { data = [], isLoading } = useTrades();
  if (isLoading) return <p className="text-gray-400">Loading reconstructed trades…</p>;
  return (
    <div>
      <h1 className="text-2xl font-semibold text-white mb-4">Trade explorer</h1>
      <table className="w-full text-sm text-left">
        <thead className="text-gray-400 border-b border-gray-800">
          <tr>
            <th className="py-2">Login</th>
            <th>Symbol</th>
            <th>Dir</th>
            <th>Opened</th>
            <th>Closed</th>
            <th>Lots</th>
            <th>Net</th>
            <th>Done</th>
          </tr>
        </thead>
        <tbody>
          {data.map((t: any) => (
            <tr key={t.id} className="border-b border-gray-800 text-gray-200">
              <td className="py-2">{t.login}</td>
              <td>{t.canonicalSymbol}</td>
              <td>{t.direction}</td>
              <td>{t.openedAt}</td>
              <td>{t.closedAt ?? 'open'}</td>
              <td>{Number(t.maxVolumeLots).toFixed(2)}</td>
              <td>{Number(t.netRealizedPnl).toFixed(2)}</td>
              <td>{t.completed ? 'yes' : 'no'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
