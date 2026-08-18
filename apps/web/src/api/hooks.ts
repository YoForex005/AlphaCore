import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import client from './client';
import type { Overview, Broker, Group, Trader, TraderDetail, FixSession, RiskStatus, ReconciliationStatus, HealthStatus, Settings } from '../types';

export function useOverview() {
  return useQuery<Overview>({ queryKey: ['overview'], queryFn: () => client.get('/api/overview').then(r => r.data) });
}

export function useBrokers() {
  return useQuery<Broker[]>({ queryKey: ['brokers'], queryFn: () => client.get('/api/brokers').then(r => r.data) });
}

export function useGroups() {
  return useQuery<Group[]>({ queryKey: ['groups'], queryFn: () => client.get('/api/groups').then(r => r.data) });
}

export interface TraderFilters { broker?: string; group?: string; state?: string; minScore?: number; maxScore?: number; martingale?: boolean; page?: number; pageSize?: number; }

export function useTraders(filters: TraderFilters) {
  return useQuery<{ items: Trader[]; total: number }>({
    queryKey: ['traders', filters],
    queryFn: () => client.get('/api/traders', { params: filters }).then(r => r.data),
  });
}

export function useTraderDetail(brokerId: string, login: string) {
  return useQuery<TraderDetail>({
    queryKey: ['trader', brokerId, login],
    queryFn: () => client.get(`/api/traders/${brokerId}/${login}`).then(r => r.data),
    enabled: !!brokerId && !!login,
  });
}

export function useFixSessions() {
  return useQuery<FixSession[]>({ queryKey: ['fix-sessions'], queryFn: () => client.get('/api/fix/sessions').then(r => r.data), refetchInterval: 5000 });
}

export function useRiskStatus() {
  return useQuery<RiskStatus>({ queryKey: ['risk'], queryFn: () => client.get('/api/risk/status').then(r => r.data), refetchInterval: 5000 });
}

export function useStopNewExecution() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: () => client.post('/api/risk/stop-new-execution'), onSuccess: () => qc.invalidateQueries({ queryKey: ['risk'] }) });
}

export function useEmergencyFlatten() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: () => client.post('/api/risk/emergency-flatten'), onSuccess: () => qc.invalidateQueries({ queryKey: ['risk'] }) });
}

export function useReconciliation() {
  return useQuery<ReconciliationStatus>({ queryKey: ['reconciliation'], queryFn: () => client.get('/api/reconciliation/status').then(r => r.data) });
}

export function useHealth() {
  return useQuery<HealthStatus>({ queryKey: ['health'], queryFn: () => client.get('/api/health').then(r => r.data), refetchInterval: 10000 });
}

export function useSettings() {
  return useQuery<Settings>({ queryKey: ['settings'], queryFn: () => client.get('/api/settings').then(r => r.data) });
}

export function useUpdateSettings() {
  const qc = useQueryClient();
  return useMutation({ mutationFn: (s: Settings) => client.put('/api/settings', s), onSuccess: () => qc.invalidateQueries({ queryKey: ['settings'] }) });
}
