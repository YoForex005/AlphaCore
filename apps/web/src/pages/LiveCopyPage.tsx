import { useCopyIntents, useCopyLive, useCopyReconcile, useCopyStatus } from '../api/hooks';

export default function LiveCopyPage() {
  const { data: status } = useCopyStatus();
  const { data: intents = [] } = useCopyIntents();
  const { data: live } = useCopyLive();
  const { data: rec } = useCopyReconcile();
  const fills = live?.fills ?? [];
  const open = live?.open ?? fills.filter((f: any) => !f.DestClosed).length;
  const closed = live?.closed ?? fills.filter((f: any) => f.DestClosed).length;

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-semibold text-white">Live copy portfolio</h1>
      <p className="text-emerald-300 text-sm">
        Dest book: demo Pepperstone {live?.dest ?? '5328266'}. Backend compares MT5 Manager tickets to dest 35=AN. Dest closes only when the master ticket is gone — not a manual flatten.
      </p>
      <p className="text-gray-500 text-xs">Updated {live?.updatedUtc ?? '—'}</p>
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3 text-sm">
        <Stat label="Dest OPEN" value={open} hot />
        <Stat label="Dest CLOSED" value={closed} />
        <Stat label="Dest fills total" value={live?.total ?? fills.length} />
        <Stat label="SHADOW (API)" value={status?.shadowTraders ?? 0} />
        <Stat label="LIVE traders" value={status?.liveTraders ?? 0} />
        <Stat label="Paper intents" value={status?.intents ?? 0} />
        <Stat label="QUOTE" value={status?.quoteLoggedOn ? 'up' : 'down'} />
        <Stat label="TRADE" value={status?.tradeLoggedOn ? 'up' : 'down'} />
        <Stat label="Master still open" value={rec?.masterStillOpen ?? '—'} />
        <Stat label="Master gone → close" value={rec?.masterGoneShouldClose ?? '—'} hot />
        <Stat label="cTrader dest open" value={rec?.destVenueOpen ?? '—'} />
        <Stat label="Dest already flat" value={rec?.destAlreadyFlat ?? '—'} />
      </div>
      <table className="w-full text-sm text-left">
        <thead className="text-gray-400 border-b border-gray-800">
          <tr>
            <th className="py-2">Broker</th>
            <th>Master</th>
            <th>MT5 pos</th>
            <th>Dest pos</th>
            <th>Side</th>
            <th>Lots</th>
            <th>Fill</th>
            <th>State</th>
          </tr>
        </thead>
        <tbody>
          {fills.slice().reverse().map((f: any) => (
            <tr key={`${f.Broker}-${f.SourceLogin}-${f.SourcePositionId}`} className="border-b border-gray-800 text-gray-200">
              <td className="py-2">{f.Broker}</td>
              <td>{f.SourceLogin}</td>
              <td>{f.SourcePositionId}</td>
              <td>{f.DestPositionId}</td>
              <td>{f.IsLong ? 'Buy' : 'Sell'}</td>
              <td>{Number(f.Lots).toFixed(2)}</td>
              <td>{f.DestFillPrice ?? '—'}</td>
              <td className={f.DestClosed ? 'text-emerald-400' : 'text-amber-300'}>{f.DestClosed ? 'CLOSED' : 'OPEN'}</td>
            </tr>
          ))}
        </tbody>
      </table>
      {fills.length === 0 && (
        <p className="text-gray-500 text-sm">No dest ledger yet. FastCopyWatch writes this table when it sends to demo cTrader.</p>
      )}
      {intents.length > 0 && (
        <p className="text-gray-600 text-xs">Paper SHADOW intents from the old API hop: {intents.length} (not dest fills).</p>
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
