import * as signalR from '@microsoft/signalr';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'api'
const HUB_BASE_URL = API_BASE_URL.replace(/\/api\/?$/, '/hubs');

export async function dbServerHealthCheck(signal) {
	const response = await fetch(`${API_BASE_URL}/health`, { signal });
	if (!response.ok) {
		let message = `Request failed with status ${response.status}`
		try {
			const text = await response.text()
			if (text) message = text
		} catch {
			// ignore parse errors and keep default message
		}
		throw new Error(message)
	}
	
	const data = await response.json();

	console.log('DbHealth:', data);
	return data;
}
