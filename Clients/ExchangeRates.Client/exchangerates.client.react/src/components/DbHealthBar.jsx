import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { useMsal } from '@azure/msal-react';
import { getAccessToken } from '../services/authToken.js';
import { dbServerHealthCheck } from '../services/healthCheckService'
import './DbHealthBar.css';

const HUB_BASE_URL = import.meta.env.VITE_HUB_BASE_URL || 'hubs';

function DbHealthBar() {
    const [status, setHealth] = useState({ canConnect: false, latencyMs: 0, error: '' });
    const { instance } = useMsal();

    useEffect(() => {
        const controller = new AbortController();

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${HUB_BASE_URL}/health`, { accessTokenFactory: () => getAccessToken(instance) })
            .withAutomaticReconnect()
            .build();

        connection.on('HealthStatusChanged', health => {
            setHealth(health);
        });

        connection.start().catch(error => console.error('Health hub connection failed:', error));

        dbServerHealthCheck(controller.signal)
            .then(data => {
                setHealth(data);
            }).catch(error => {
                if (error.name !== 'AbortError') {
                    console.error('Error fetching DbHealth:', error);
                }
            });

        return () => {
            controller.abort();
            connection.stop();
        };
    }, [instance]);

    return (
        <div className="db-health-bar">
            <div>Database status </div>
                    <div>Connection {status.canConnect ? '🟢' : '🔴'}</div>
                    <div>Latency: {status.latencyMs} ms</div>
                    {status.error && <div>-Error: {status.error}</div>}
        </div>
    );
}

export default DbHealthBar;
