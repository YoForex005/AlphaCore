export function formatPrice(price: number, decimals = 2): string {
  return price.toFixed(decimals);
}

export function formatVolume(lots: number): string {
  return lots.toFixed(2);
}

export function timeAgo(iso: string): string {
  if (!iso) return '—';
  const diff = Date.now() - new Date(iso).getTime();
  const seconds = Math.floor(diff / 1000);
  if (seconds < 60) return `${seconds}s ago`;
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  return `${Math.floor(hours / 24)}d ago`;
}

export function pnlColor(pnl: number): string {
  if (pnl > 0) return 'text-green-400';
  if (pnl < 0) return 'text-red-400';
  return 'text-gray-400';
}

export function pnlBg(pnl: number): string {
  if (pnl > 0) return 'bg-green-900/30';
  if (pnl < 0) return 'bg-red-900/30';
  return '';
}
