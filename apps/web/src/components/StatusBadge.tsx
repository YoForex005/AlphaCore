interface Props { status: string; }

const colors: Record<string, string> = {
  healthy: 'bg-green-600/20 text-green-300',
  connected: 'bg-green-600/20 text-green-300',
  active: 'bg-green-600/20 text-green-300',
  enabled: 'bg-green-600/20 text-green-300',
  warning: 'bg-yellow-600/20 text-yellow-300',
  error: 'bg-red-600/20 text-red-300',
  disconnected: 'bg-red-600/20 text-red-300',
  disabled: 'bg-gray-600/20 text-gray-400',
};

export default function StatusBadge({ status }: Props) {
  const cls = colors[status.toLowerCase()] || 'bg-gray-600/20 text-gray-300';
  return <span className={`inline-block px-2 py-0.5 rounded text-xs font-medium ${cls}`}>{status}</span>;
}
