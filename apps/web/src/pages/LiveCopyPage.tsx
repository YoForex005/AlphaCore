import { useCopyIntents, useCopyStatus } from '../api/hooks';

export default function LiveCopyPage() {
  const { data: status, isLoading } = useCopyStatus();
  const { data: intents = [] } = useCopyIntents();
  if (isLoading) return <p className="text-gray-400">Loading copy pipeline…</p>;

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-semibold text-white">Live copy portfolio</h1>
      <p className="text-amber-300 text-sm">{status?.summary}</p>
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3 text-sm">
        <Stat label="REAL_COPY armed" value={status?.realCopyArmed ? 'YES' : 'NO'} hot={status?.realCopyArmed} />
        <Stat label="SHADOW traders" value={status?.shadowTraders ?? 0} />
        <Stat label="LIVE traders" value={status?.liveTraders ?? 0} />
        <Stat label="Live sends" value={status?.liveSends ?? 0} />
        <Stat label="Intents" value={status?.intents ?? 0} />
        <Stat label="Shadow fills" value={status?.shadowFills ?? 0} />
        <Stat label="QUOTE" value={status?.quoteLoggedOn ? 'up' : 'down'} />
        <Stat label="TRADE" value={status?.tradeLoggedOn ? 'up' : 'down'} />
      </div>
      {status?.blockers?.length > 0 && (
        <div className="rounded border border-amber-900 bg-amber-950/40 p-3 text-sm text-amber-200">
          <div className="font-medium mb-1">Live send blockers (Pepperstone cannot be filled)</div>
          <ul className="list-disc pl-5 space-y-1">
            {status.blockers.map((b: string) => <li key={b}>{b}</li>)}
          </ul>
        </div>
      )}
      <table className="w-full text-sm text-left">
        <thead className="text-gray-400 border-b border-gray-800">
          <tr>
            <th className="py-2">Broker</th>
            <th>Login</th>
            <th>Pos</th>
            <th>Side</th>
            <th>Qty</th>
            <th>Status</th>
            <th>Risk</th>
          </tr>
        </thead>
        <tbody>
          {intents.map((c: any) => (
            <tr key={`${c.broker}-${c.login}-${c.positionId}`} className="border-b border-gray-800 text-gray-200">
              <td className="py-2">{c.broker}</td>
              <td>{c.login}</td>
              <td>{c.positionId}</td>
              <td>{c.direction}</td>
              <td>{Number(c.quantity).toFixed(2)}</td>
              <td>{c.status}</td>
              <td>{c.riskReason ?? '—'}</td>
            </tr>
          ))}
        </tbody>
      </table>
      {intents.length === 0 && (
        <p className="text-gray-500 text-sm">No SHADOW/LIVE_CANDIDATE traders with completed XAU trades yet. Intents appear after scoring promotes someone to SHADOW.</p>
      )}
    </div>
  );
}

function Stat({ label, value, hot }: { label: string; value: string | number; hot?: boolean }) {
  return (
    <div className="rounded border border-gray-800 bg-gray-950 p-3">
      <div className="text-gray-400 text-xs">{label}</div>
      <div className={hot ? 'text-amber-300 font-semibold' : 'text-white'}>{value}</div>
    </div>
  );
}
