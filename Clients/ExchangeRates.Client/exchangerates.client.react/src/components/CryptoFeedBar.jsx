import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';
//import { dbServerHealthCheck } from '../services/healthCheckService'

const HUB_BASE_URL = import.meta.env.VITE_HUB_BASE_URL || 'hubs';

console.log('HUB_BASE_URL:', HUB_BASE_URL);

function CryptoFeedBar() {
    const [status, setStatus] = useState({ Symbol: '', Price: 0, TimestampUtc: new Date(), Provider: '' });

    async function fetchCryptoStatus() {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${HUB_BASE_URL}/crypto`)
            .withAutomaticReconnect()
            .build();

        connection.on('CryptoValueChanged', crypto => {
            console.log('Crypto -> ', crypto);
            setStatus(crypto);
        });

        await connection.start();
    }

    useEffect(() => {
      const controller = new AbortController();
       fetchCryptoStatus();
        //dbServerHealthCheck(controller.signal)
        //    .then(data => {
        //        setStatus(data);
        //    }).catch(error => {
        //        if (error.name !== 'AbortError') {
        //            console.error('Error fetching CryptoStatus:', error);
        //        }
        //    });
        return () => controller.abort();
    }, []);

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