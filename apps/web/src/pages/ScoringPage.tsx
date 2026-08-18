import { useTraders } from '../api/hooks';

export default function ScoringPage() {
  const { data = [] } = useTraders({});
  return (
    <div>
      <h1 className="text-2xl font-semibold text-white mb-2">Deterministic scoring</h1>
      <p className="text-sm text-gray-400 mb-4">XGBoost is not active. ML must beat this baseline out of sample before it is used.</p>
      <table className="w-full text-sm">
        <thead className="text-gray-400 border-b border-gray-800">
          <tr>
            <th className="py-2 text-left">Trader</th>
            <th className="text-left">Early quality</th>
            <th className="text-left">Behavior</th>
            <th className="text-left">Risk</th>
            <th className="text-left">State</th>
          </tr>
        </thead>
        <tbody>
          {data.map((t: any) => (
            <tr key={`${t.broker}-${t.login}`} className="border-b border-gray-800 text-gray-200">
              <td className="py-2">{t.broker}:{t.login}</td>
              <td>{Number(t.earlyScore).toFixed(1)}</td>
              <td>{Number(t.behaviorScore ?? 0).toFixed(1)}</td>
              <td>{Number(t.riskScore).toFixed(1)}</td>
              <td>{t.state}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
