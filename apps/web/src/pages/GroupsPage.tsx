import { useGroups, useIngestStatus } from '../api/hooks';

export default function GroupsPage() {
  const { data = [], isLoading } = useGroups();
  const ingest = useIngestStatus();
  if (isLoading) return <p className="text-gray-400">Loading groups…</p>;
  return (
    <div>
      <h1 className="text-2xl font-semibold text-white mb-2">MT5 Groups</h1>
      <p className="text-sm text-gray-400 mb-4">Every group visible to the Achiever and Starwave managers. Count: {data.length}.</p>
      {ingest.data?.brokers && (
        <p className="text-xs text-gray-500 mb-3">{JSON.stringify(ingest.data.brokers)}</p>
      )}
      <table className="w-full text-sm text-left">
        <thead className="text-gray-400 border-b border-gray-800">
          <tr>
            <th className="py-2">Broker</th>
            <th>Group</th>
            <th>Accounts</th>
            <th>Analysis</th>
            <th>Plan</th>
          </tr>
        </thead>
        <tbody>
          {data.map((g: any) => (
            <tr key={`${g.broker}-${g.group}`} className="border-b border-gray-800 text-gray-200">
              <td className="py-2">{g.broker}</td>
              <td>{g.group}</td>
              <td>{g.accounts}</td>
              <td>{g.enabledForAnalysis ? 'yes' : 'no'}</td>
              <td>{g.planMapping ?? '—'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
