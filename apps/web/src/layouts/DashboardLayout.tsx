import { NavLink, Outlet } from 'react-router-dom';
import { useEffect } from 'react';
import { startConnection } from '../api/signalr';

const nav = [
  { to: '/overview', label: 'Overview', icon: '◈' },
  { to: '/brokers', label: 'Brokers', icon: '⛁' },
  { to: '/groups', label: 'Groups', icon: '⊞' },
  { to: '/traders', label: 'Traders', icon: '⚑' },
  { to: '/trades', label: 'Trades', icon: '⇄' },
  { to: '/scoring', label: 'Scoring', icon: '★' },
  { to: '/shadow', label: 'Shadow', icon: '◐' },
  { to: '/fix', label: 'FIX', icon: '⚡' },
  { to: '/risk', label: 'Risk', icon: '⚠' },
  { to: '/reconciliation', label: 'Recon', icon: '⟳' },
  { to: '/health', label: 'Health', icon: '♥' },
  { to: '/settings', label: 'Settings', icon: '⚙' },
];

export default function DashboardLayout() {
  useEffect(() => { startConnection(); }, []);

  return (
    <div className="flex h-screen overflow-hidden">
      <aside className="w-56 bg-gray-950 border-r border-gray-800 flex flex-col">
        <div className="px-4 py-5 text-lg font-bold tracking-wide text-blue-400">MT5 Intelligence</div>
        <nav className="flex-1 overflow-y-auto px-2 space-y-0.5">
          {nav.map(n => (
            <NavLink key={n.to} to={n.to} className={({ isActive }) =>
              `flex items-center gap-3 px-3 py-2 rounded text-sm ${isActive ? 'bg-blue-600/20 text-blue-300' : 'text-gray-400 hover:text-gray-200 hover:bg-gray-800'}`
            }>
              <span className="text-base">{n.icon}</span>{n.label}
            </NavLink>
          ))}
        </nav>
      </aside>
      <main className="flex-1 overflow-y-auto p-6 bg-gray-900">
        <Outlet />
      </main>
    </div>
  );
}
