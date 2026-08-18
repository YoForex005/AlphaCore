import { useQuery } from '@tanstack/react-query';
import client from './client';

export function useOverview() {
  return useQuery({ queryKey: ['overview'], queryFn: () => client.get('/api/overview').then(r => r.data), refetchInterval: 4000 });
}

export function useBrokers() {
  return useQuery({ queryKey: ['brokers'], queryFn: () => client.get('/api/brokers').then(r => r.data), refetchInterval: 4000 });
}

export function useGroups() {
  return useQuery({ queryKey: ['groups'], queryFn: () => client.get('/api/groups').then(r => r.data), refetchInterval: 4000 });
}

export function useIngestStatus() {
  return useQuery({ queryKey: ['ingest-status'], queryFn: () => client.get('/api/ingest/status').then(r => r.data), refetchInterval: 2000 });
}

export function useTraders(filters: { broker?: string; state?: string }) {
  return useQuery({
    queryKey: ['traders', filters],
    queryFn: () => client.get('/api/traders', { params: filters }).then(r => r.data),
    refetchInterval: 5000,
  });
}

export function useTraderDetail(broker: string, login: string) {
  return useQuery({
    queryKey: ['trader', broker, login],
    queryFn: () => client.get(`/api/traders/${broker}/${login}`).then(r => r.data),
    enabled: !!broker && !!login,
  });
}

export function useTrades() {
  return useQuery({ queryKey: ['trades'], queryFn: () => client.get('/api/trades').then(r => r.data) });
}

export function useFixSessions() {
  return useQuery({ queryKey: ['fix-sessions'], queryFn: () => client.get('/api/fix/sessions').then(r => r.data), refetchInterval: 5000 });
}

export function useRiskStatus() {
  return useQuery({ queryKey: ['risk'], queryFn: () => client.get('/api/risk').then(r => r.data), refetchInterval: 5000 });
}

export function useReconciliation() {
  return useQuery({ queryKey: ['reconciliation'], queryFn: () => client.get('/api/reconciliation/status').then(r => r.data) });
}

export function useHealth() {
  return useQuery({ queryKey: ['health'], queryFn: () => client.get('/api/health').then(r => r.data), refetchInterval: 10000 });
}

export function useSettings() {
  return useQuery({ queryKey: ['settings'], queryFn: () => client.get('/api/settings').then(r => r.data) });
}

export function useCopyStatus() {
  return useQuery({ queryKey: ['copy-status'], queryFn: () => client.get('/api/copy/status').then(r => r.data), refetchInterval: 3000 });
}

export function useCopyIntents() {
  return useQuery({ queryKey: ['copy-intents'], queryFn: () => client.get('/api/copy/intents').then(r => r.data), refetchInterval: 4000 });
}
