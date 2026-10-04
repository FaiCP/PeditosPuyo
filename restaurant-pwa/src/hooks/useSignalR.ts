import { useEffect, useRef, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';

export function useSignalR(hubUrl: string, groupName: string, groupType: 'company' | 'rider' | 'restaurant') {
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${hubUrl}`, {
        accessTokenFactory: () => localStorage.getItem('token') || '',
      })
      .withAutomaticReconnect()
      .build();

    connection.start()
      .then(() => {
        console.log('SignalR connected');
        if (groupType === 'company') {
          connection.invoke('JoinCompany', groupName);
        } else if (groupType === 'restaurant') {
          connection.invoke('JoinRestaurant', groupName);
        } else if (groupType === 'rider') {
          connection.invoke('JoinRider', groupName);
        }
      })
      .catch((err) => console.error('SignalR connection error:', err));

    connectionRef.current = connection;

    return () => {
      connection.stop();
    };
  }, [hubUrl, groupName, groupType]);

  const on = useCallback((event: string, callback: (...args: unknown[]) => void) => {
    connectionRef.current?.on(event, callback);
  }, []);

  const send = useCallback((method: string, ...args: unknown[]) => {
    connectionRef.current?.invoke(method, ...args);
  }, []);

  return { on, send };
}
