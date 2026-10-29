import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { useMsal } from '@azure/msal-react';
import { getAccessToken } from '../services/authToken.js';
import { dbServerHealthCheck } from '../services/healthCheckService'
import { useHubConnectionState } from './statusHooks.js';
import './StatusCards.css';

const HUB_BASE_URL = import.meta.env.VITE_HUB_BASE_URL || 'hubs';

function latencyLevel(ms) {
    if (ms < 50) return 'good';
    if (ms < 200) return 'fair';
    return 'poor';
}

function DbHealthBar() {
    const [health, setHealth] = useState(null);
    const { instance } = useMsal();
    const [hubState, trackConnection] = useHubConnectionState();

    useEffect(() => {
        const controller = new AbortController();

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${HUB_BASE_URL}/health`, { accessTokenFactory: () => getAccessToken(instance) })
            .withAutomaticReconnect()
            .build();

        connection.on('HealthStatusChanged', setHealth);

        const start = trackConnection(connection);
        start().catch(error => console.error('Health hub connection failed:', error));

        dbServerHealthCheck(controller.signal)
            .then(setHealth)
            .catch(error => {
                if (error.name !== 'AbortError') {
                    console.error('Error fetching DbHealth:', error);
                    setHealth({ canConnect: false, latencyMs: 0, error: 'Health check unavailable' });
                }
            });

        return () => {
            controller.abort();
            connection.stop();
        };
    }, [instance, trackConnection]);

    const state = !health ? 'connecting' : health.canConnect ? 'live' : 'offline';
    const label = { connecting: 'Checking', live: 'Online', offline: 'Offline' }[state];

    return (
        <section className="status-card db-card" aria-label="Database status">
            <div className="status-card-header">
                <span className="status-card-title">Database</span>
                <span className={`status-pill status-${state}`} role="status">
                    <span className="status-dot" aria-hidden="true" />
                    {label}
                </span>
            </div>

            <div className="db-latency">
                {health?.canConnect ? (
                    <>
                        <span className={`db-latency-value latency-${latencyLevel(health.latencyMs)}`}>
                            {health.latencyMs.toFixed(1)}
                        </span>
                        <span className="db-latency-unit">ms latency</span>
                    </>
                ) : (
                    <span className="db-latency-value is-placeholder">—</span>
                )}
            </div>

            <div className="status-card-meta">
                {health?.error
                    ? <span className="status-error" title={health.error}>{health.error}</span>
                    : <span>{{
                        live: 'Live updates every 10s',
                        connecting: 'Connecting to live updates…',
                        reconnecting: 'Reconnecting to live updates…',
                        offline: 'Live updates unavailable'
                    }[hubState]}</span>}
            </div>
        </section>
    );
}

export default DbHealthBar;
