import { formatPrice } from '../utils/formatters';

interface Props { bid: number; ask: number; spread: number; age?: number; }

export default function QuoteDisplay({ bid, ask, spread, age }: Props) {
  const stale = age != null && age > 5000;
  return (
    <div className={`flex gap-4 items-center font-mono text-sm ${stale ? 'text-yellow-400' : 'text-gray-100'}`}>
      <span>Bid: <strong>{formatPrice(bid, 2)}</strong></span>
      <span>Ask: <strong>{formatPrice(ask, 2)}</strong></span>
      <span className="text-gray-400">Spread: {formatPrice(spread, 1)}</span>
      {age != null && <span className="text-xs text-gray-500">{age}ms</span>}
    </div>
  );
}
