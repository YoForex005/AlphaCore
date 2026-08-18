import { useBrokers } from '../api/hooks';

export default function BrokersPage() {
  const { data = [], isLoading } = useBrokers();
  if (isLoading) return <p className="text-gray-400">Loading brokers…</p>;
  return (
    <div>
      <h1 className="text-2xl font-semibold text-white mb-4">Brokers</h1>
      <table className="w-full text-sm text-left">
        <thead className="text-gray-400 border-b border-gray-800">
          <tr>
            <th className="py-2">Code</th>
            <th>Name</th>
            <th>Server</th>
            <th>Manager</th>
            <th>Groups</th>
            <th>Accounts</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          {data.map((b: any) => (
            <tr key={b.code} className="border-b border-gray-800 text-gray-200">
              <td className="py-2">{b.code}</td>
              <td>{b.displayName}</td>
              <td>{b.server}</td>
              <td>{b.managerLoginMasked}**</td>
              <td>{b.groupCount}</td>
              <td>{b.accountCount}</td>
              <td className={b.connected ? 'text-emerald-300' : 'text-red-400'}>{b.connected ? 'connected' : 'down'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
