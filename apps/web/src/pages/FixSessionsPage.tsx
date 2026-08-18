import { useFixSessions } from '../api/hooks';

export default function FixSessionsPage() {
  const { data = [] } = useFixSessions();
  return (
    <div>
      <h1 className="text-2xl font-semibold text-white mb-2">cTrader FIX</h1>
      <p className="text-sm text-gray-400 mb-4">QUOTE and TRADE are independent sessions. Password is never shown. TargetCompID stays <code>cServer</code>.</p>
      <div className="grid md:grid-cols-2 gap-4">
        {data.map((s: any) => (
          <div key={s.qualifier} className="bg-gray-800 border border-gray-700 rounded p-4 space-y-1 text-sm text-gray-200">
            <div className="text-lg font-semibold text-blue-300">{s.qualifier}</div>
            <div>{s.host}:{s.port}</div>
            <div>Connected: {String(s.connected)} · Logged on: {String(s.loggedOn)}</div>
            <div>Status: {s.status}</div>
            <div>Seq in/out: {s.inboundSeq} / {s.outboundSeq}</div>
            <div>Reconnects: {s.reconnectCount}</div>
            {s.bid != null && <div>Bid/Ask: {s.bid} / {s.ask} · age {Number(s.quoteAgeSeconds ?? 0).toFixed(1)}s</div>}
            <div>Instrument ID: {s.instrumentId ?? 'not discovered yet'}</div>
            <div>Execution enabled: {String(s.executionEnabled)}</div>
          </div>
        ))}
      </div>
    </div>
  );
}
