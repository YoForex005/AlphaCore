import { useReconciliation } from '../api/hooks';

export default function ReconciliationPage() {
  const { data } = useReconciliation();
  return (
    <div>
      <h1 className="text-2xl font-semibold text-white mb-2">Reconciliation</h1>
      <p className="text-sm text-gray-400 mb-4">Unresolved venue differences block new execution.</p>
      <pre className="bg-gray-950 border border-gray-800 rounded p-4 text-sm text-gray-200">{JSON.stringify(data, null, 2)}</pre>
    </div>
  );
}
