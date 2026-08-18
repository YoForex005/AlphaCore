import { Routes, Route, Navigate } from 'react-router-dom';
import DashboardLayout from './layouts/DashboardLayout';
import OverviewPage from './pages/OverviewPage';
import BrokersPage from './pages/BrokersPage';
import GroupsPage from './pages/GroupsPage';
import TradersPage from './pages/TradersPage';
import TraderDetailPage from './pages/TraderDetailPage';
import TradeExplorerPage from './pages/TradeExplorerPage';
import ScoringPage from './pages/ScoringPage';
import ShadowPortfolioPage from './pages/ShadowPortfolioPage';
import LiveCopyPage from './pages/LiveCopyPage';
import FixSessionsPage from './pages/FixSessionsPage';
import RiskPage from './pages/RiskPage';
import ReconciliationPage from './pages/ReconciliationPage';
import SystemHealthPage from './pages/SystemHealthPage';
import AuditPage from './pages/AuditPage';
import SettingsPage from './pages/SettingsPage';

export default function App() {
  return (
    <Routes>
      <Route element={<DashboardLayout />}>
        <Route index element={<Navigate to="/overview" replace />} />
        <Route path="overview" element={<OverviewPage />} />
        <Route path="brokers" element={<BrokersPage />} />
        <Route path="groups" element={<GroupsPage />} />
        <Route path="traders" element={<TradersPage />} />
        <Route path="traders/:brokerId/:login" element={<TraderDetailPage />} />
        <Route path="trades" element={<TradeExplorerPage />} />
        <Route path="scoring" element={<ScoringPage />} />
        <Route path="shadow" element={<ShadowPortfolioPage />} />
        <Route path="live" element={<LiveCopyPage />} />
        <Route path="fix" element={<FixSessionsPage />} />
        <Route path="risk" element={<RiskPage />} />
        <Route path="reconciliation" element={<ReconciliationPage />} />
        <Route path="health" element={<SystemHealthPage />} />
        <Route path="audit" element={<AuditPage />} />
        <Route path="settings" element={<SettingsPage />} />
      </Route>
    </Routes>
  );
}
