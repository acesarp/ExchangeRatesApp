import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { loginRequest } from '../services/authConfig.js'
import { useMsal } from '@azure/msal-react';
import { dbServerHealthCheck } from '../services/healthCheckService'
import './DbHealthBar.css';

const HUB_BASE_URL = import.meta.env.VITE_HUB_BASE_URL || 'hubs';

console.log('HUB_BASE_URL:', HUB_BASE_URL);

function DbHealthBar() {
    const [status, setHealth] = useState({ canConnect: false, latencyMs: 0, error: '' });
    const { instance, accounts } = useMsal();

    async function fetchHealthStatus() {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${HUB_BASE_URL}/health`, {
                accessTokenFactory: async () => {
                    const response = await instance.acquireTokenSilent({ ...loginRequest, account: accounts[0] });
                    return response.accessToken;
                }
            })
            .withAutomaticReconnect()
            .build();

        connection.on('HealthStatusChanged', health => {
            //console.log('Health -> ', health);
            setHealth(health);
        });

        await connection.start();
    }

    useEffect(() => {
        const controller = new AbortController();
        fetchHealthStatus();
        dbServerHealthCheck(controller.signal)
            .then(data => {
                setHealth(data);
        }).catch(error => {
            if (error.name !== 'AbortError') {
                console.error('Error fetching DbHealth:', error);
            }
        });
        return () => controller.abort();
    }, []);

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