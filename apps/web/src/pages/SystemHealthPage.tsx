import { useHealth } from '../api/hooks';

export default function SystemHealthPage() {
  const { data } = useHealth();
  return (
    <div>
      <h1 className="text-2xl font-semibold text-white mb-4">System health</h1>
      <pre className="bg-gray-950 border border-gray-800 rounded p-4 text-sm text-gray-200">{JSON.stringify(data, null, 2)}</pre>
    </div>
  );
}
