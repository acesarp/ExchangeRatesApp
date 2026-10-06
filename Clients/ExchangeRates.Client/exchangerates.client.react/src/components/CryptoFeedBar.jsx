import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { useMsal } from '@azure/msal-react';
import { getAccessToken } from '../services/authToken.js';

const HUB_BASE_URL = import.meta.env.VITE_HUB_BASE_URL || 'hubs';

function CryptoFeedBar() {
    const [status, setStatus] = useState({ Symbol: '', Price: 0, TimestampUtc: new Date(), Provider: '' });
    const { instance } = useMsal();

    useEffect(() => {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${HUB_BASE_URL}/crypto`, { accessTokenFactory: () => getAccessToken(instance) })
            .withAutomaticReconnect()
            .build();

        connection.on('CryptoPriceUpdated', crypto => {
            setStatus(crypto);
        });

        connection.start().catch(error => console.error('Crypto hub connection failed:', error));

        return () => {
            connection.stop();
        };
    }, [instance]);

    return (
        <div className="crypto-feed-bar">
            <div>Crypto status </div>
            <div>Symbol: {status.Symbol}</div>
            <div>Price: {status.Price}</div>
            <div>Timestamp: {status.TimestampUtc.toString()}</div>
            <div>Provider: {status.Provider}</div>
        </div>
    );
}

export default CryptoFeedBar;
