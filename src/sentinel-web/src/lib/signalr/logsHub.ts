import * as signalR from '@microsoft/signalr';
import type { LogEntry } from '@/types/api';

export type LogReceivedHandler = (log: LogEntry) => void;

let connection: signalR.HubConnection | null = null;

interface LogStreamPayload {
  id: string;
  tenantId: string;
  timestamp: string;
  service: string;
  environment: string;
  level: string;
  message: string;
  traceId?: string;
  correlationId?: string;
}

function mapStreamPayload(payload: LogStreamPayload): LogEntry {
  return {
    id: payload.id,
    timestamp: payload.timestamp,
    level: payload.level,
    message: payload.message,
    service: payload.service,
    traceId: payload.traceId,
  };
}

export async function connectLogsHub(
  getAccessToken: () => string | null,
  tenantId: string | null,
  onLogReceived: LogReceivedHandler,
): Promise<signalR.HubConnection> {
  if (connection?.state === signalR.HubConnectionState.Connected) {
    return connection;
  }

  connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/logs', {
      accessTokenFactory: () => getAccessToken() ?? '',
    })
    .withAutomaticReconnect()
    .build();

  connection.on('LogReceived', (payload: LogStreamPayload) => {
    onLogReceived(mapStreamPayload(payload));
  });

  await connection.start();

  if (tenantId) {
    await connection.invoke('Subscribe', tenantId);
  }

  return connection;
}

export async function disconnectLogsHub(): Promise<void> {
  if (connection) {
    await connection.stop();
    connection = null;
  }
}
