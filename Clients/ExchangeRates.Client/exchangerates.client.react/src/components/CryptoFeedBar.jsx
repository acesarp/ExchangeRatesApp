import { useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { useMsal } from '@azure/msal-react';
import { getAccessToken } from '../services/authToken.js';
import { useHubConnectionState, useNow } from './statusHooks.js';
import './StatusCards.css';

const HUB_BASE_URL = import.meta.env.VITE_HUB_BASE_URL || 'hubs';

// A tick older than this is shown as stale even if the hub is still connected.
const STALE_AFTER_MS = 60_000;

function formatPrice(price, symbol) {
    const quoteCurrency = symbol?.split('-')[1] ?? 'USD';
    try {
        return new Intl.NumberFormat(undefined, { style: 'currency', currency: quoteCurrency }).format(price);
    } catch {
        return price.toLocaleString();
    }
}

function formatAge(ms) {
    const seconds = Math.max(0, Math.round(ms / 1000));
    if (seconds < 60) return `${seconds}s ago`;
    const minutes = Math.round(seconds / 60);
    return minutes < 60 ? `${minutes}m ago` : `${Math.round(minutes / 60)}h ago`;
}

function CryptoFeedBar() {
    // SignalR serializes payloads in camelCase: { symbol, price, timestampUtc, provider }.
    const [tick, setTick] = useState(null);
    const [direction, setDirection] = useState('flat');
    const lastPrice = useRef(null);
    const { instance } = useMsal();
    const [hubState, trackConnection] = useHubConnectionState();
    const now = useNow(1000);

    useEffect(() => {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${HUB_BASE_URL}/crypto`, { accessTokenFactory: () => getAccessToken(instance) })
            .withAutomaticReconnect()
            .build();

        connection.on('CryptoPriceUpdated', crypto => {
            if (lastPrice.current !== null && crypto.price !== lastPrice.current) {
                setDirection(crypto.price > lastPrice.current ? 'up' : 'down');
            }
            lastPrice.current = crypto.price;
            setTick(crypto);
        });

        const start = trackConnection(connection);
        start().catch(error => console.error('Crypto hub connection failed:', error));

        return () => {
            connection.stop();
        };
    }, [instance, trackConnection]);

    const ageMs = tick ? now - new Date(tick.timestampUtc).getTime() : null;
    const isStale = ageMs !== null && ageMs > STALE_AFTER_MS;
    const indicator = hubState !== 'live' ? hubState : (!tick || isStale ? 'waiting' : 'live');
    const indicatorLabel = { live: 'Live', waiting: tick ? 'Stale' : 'Waiting', connecting: 'Connecting', reconnecting: 'Reconnecting', offline: 'Offline' }[indicator];
    const [base, quote] = tick?.symbol?.split('-') ?? ['BTC', 'USD'];

    return (
        <section className="status-card crypto-card" aria-label="Bitcoin price">
            <div className="status-card-header">
                <span className="status-card-title">{base} / {quote}</span>
                <span className={`status-pill status-${indicator}`}>
                    <span className="status-dot" aria-hidden="true" />
                    {indicatorLabel}
                </span>
            </div>

            <div className="crypto-price-row">
                <span className={`crypto-price ${tick ? '' : 'is-placeholder'}`}>
                    {tick ? formatPrice(tick.price, tick.symbol) : '—'}
                </span>
                {tick && direction !== 'flat' && (
                    <span className={`crypto-direction crypto-${direction}`} aria-label={direction === 'up' ? 'Price went up' : 'Price went down'}>
                        {direction === 'up' ? '▲' : '▼'}
                    </span>
                )}
            </div>

            <div className="status-card-meta">
                {tick ? (
                    <>
                        <span>via {tick.provider}</span>
                        <span aria-hidden="true">·</span>
                        <time dateTime={tick.timestampUtc} title={new Date(tick.timestampUtc).toLocaleString()}>
                            updated {formatAge(ageMs)}
                        </time>
                    </>
                ) : (
                    <span>Waiting for the first price update…</span>
                )}
            </div>
        </section>
    );
}

export default CryptoFeedBar;
