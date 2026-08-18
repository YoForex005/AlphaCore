interface Props { score: number; max?: number; }

export default function ScoreGauge({ score, max = 100 }: Props) {
  const pct = Math.min(100, (score / max) * 100);
  const color = pct >= 70 ? 'bg-green-500' : pct >= 40 ? 'bg-yellow-500' : 'bg-red-500';
  return (
    <div className="flex items-center gap-2">
      <div className="w-20 h-2 bg-gray-700 rounded-full overflow-hidden">
        <div className={`h-full rounded-full ${color}`} style={{ width: `${pct}%` }} />
      </div>
      <span className="text-xs text-gray-300">{score}</span>
    </div>
  );
}
