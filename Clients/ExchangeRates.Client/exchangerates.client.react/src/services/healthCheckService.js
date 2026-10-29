import * as signalR from '@microsoft/signalr';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'api'
console.log('API_BASE_URL:', API_BASE_URL);

export async function dbServerHealthCheck(signal) {
	const response = await fetch(`${API_BASE_URL}/AppHealth/status-check`, { signal });
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
