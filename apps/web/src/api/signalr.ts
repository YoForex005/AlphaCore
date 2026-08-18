import * as signalR from '@microsoft/signalr';

const BASE = import.meta.env.VITE_API_URL || 'http://localhost:5000';

let connection: signalR.HubConnection | null = null;

export function getConnection(): signalR.HubConnection {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(`${BASE}/hubs/dashboard`)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();
  }
  return connection;
}

export async function startConnection() {
  const conn = getConnection();
  if (conn.state === signalR.HubConnectionState.Disconnected) {
    try { await conn.start(); } catch (e) { console.warn('SignalR connection failed', e); }
  }
}

export function onEvent<T>(event: string, handler: (data: T) => void) {
  getConnection().on(event, handler);
  return () => getConnection().off(event, handler);
}
