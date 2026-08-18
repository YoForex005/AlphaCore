import { ResponsiveContainer, AreaChart, Area, XAxis, YAxis, Tooltip, CartesianGrid } from 'recharts';

interface Props { data: { date: string; pnl: number }[]; }

export default function PnlChart({ data }: Props) {
  if (!data.length) return <div className="text-gray-500 text-sm py-8 text-center">No data</div>;
  return (
    <ResponsiveContainer width="100%" height={250}>
      <AreaChart data={data}>
        <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
        <XAxis dataKey="date" stroke="#6b7280" tick={{ fontSize: 11 }} />
        <YAxis stroke="#6b7280" tick={{ fontSize: 11 }} />
        <Tooltip contentStyle={{ background: '#1f2937', border: '1px solid #374151', borderRadius: 8 }} />
        <Area type="monotone" dataKey="pnl" stroke="#3b82f6" fill="#3b82f680" />
      </AreaChart>
    </ResponsiveContainer>
  );
}
