export interface Overview {
  totalAccounts: number;
  totalBrokers: number;
  tradersByState: Record<string, number>;
  shadowPnl: number;
  realPnl: number;
  fixHealthy: boolean;
}

export interface Broker {
  id: string;
  name: string;
  status: string;
  server: string;
  groups: number;
  accounts: number;
  lastEvent: string;
}

export interface Group {
  brokerId: string;
  brokerName: string;
  name: string;
  accounts: number;
  enabled: boolean;
  planMapping: string;
  lastSynced: string;
}

export interface Trader {
  brokerId: string;
  login: number;
  group: string;
  completedTrades: number;
  pnl: number;
  score: number;
  riskFlags: string[];
  state: string;
  martingale: boolean;
}

export interface TraderDetail extends Trader {
  trades: Trade[];
  scoreHistory: { date: string; score: number }[];
  lotHistory: { date: string; lots: number }[];
  shadowPositions: Position[];
  livePositions: Position[];
}

export interface Trade {
  ticket: number;
  symbol: string;
  direction: string;
  lots: number;
  openPrice: number;
  closePrice: number;
  pnl: number;
  openTime: string;
  closeTime: string;
  isFirst3: boolean;
}

export interface Position {
  ticket: number;
  symbol: string;
  direction: string;
  lots: number;
  openPrice: number;
  currentPrice: number;
  pnl: number;
}

export interface FixSession {
  type: 'QUOTE' | 'TRADE';
  host: string;
  port: number;
  connected: boolean;
  loggedOn: boolean;
  inSequence: number;
  outSequence: number;
  lastHeartbeat: string;
  errors: number;
  instrumentId?: string;
  bid?: number;
  ask?: number;
  spread?: number;
  quoteAge?: number;
  executionEnabled?: boolean;
  openOrders?: number;
  openPositions?: number;
  lastExecutionReport?: string;
}

export interface RiskStatus {
  equity: number;
  balance: number;
  margin: number;
  dailyPnl: number;
  drawdown: number;
  xauExposureLong: number;
  xauExposureShort: number;
  xauExposureNet: number;
  riskByTrader: { login: number; risk: number }[];
  rejectedIntents: { login: number; reason: string; time: string }[];
  stopNewExecution: boolean;
  emergencyFlatten: boolean;
}

export interface ReconciliationStatus {
  lastReconciliation: string;
  unknownPositions: number;
  mismatches: number;
  orphanFills: number;
}

export interface HealthStatus {
  mt5Connections: ComponentHealth[];
  fixSessions: ComponentHealth[];
  database: ComponentHealth;
  redis: ComponentHealth;
  outboxBacklog: number;
}

export interface ComponentHealth {
  name: string;
  healthy: boolean;
  lastCheck: string;
  details?: string;
}

export interface Settings {
  riskLimits: Record<string, number>;
  featureFlags: Record<string, boolean>;
  brokerConfigs: { id: string; name: string; enabled: boolean }[];
}
