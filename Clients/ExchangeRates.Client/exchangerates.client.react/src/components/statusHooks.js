import { useCallback, useEffect, useRef, useState } from 'react';

/** Re-renders every `intervalMs` and returns the current time in ms, for "updated Xs ago" labels. */
export function useNow(intervalMs) {
	const [now, setNow] = useState(() => Date.now());

	useEffect(() => {
		const id = setInterval(() => setNow(Date.now()), intervalMs);
		return () => clearInterval(id);
	}, [intervalMs]);

	return now;
}

/**
 * Tracks a SignalR connection as 'connecting' | 'live' | 'reconnecting' | 'offline'.
 * `track(connection)` wires the lifecycle events and returns a `start()` to use instead of `connection.start()`.
 * Events from a previous connection (e.g. one stopped by an effect cleanup) are ignored.
 */
export function useHubConnectionState() {
	const [state, setState] = useState('connecting');
	const current = useRef(null);

	const track = useCallback(connection => {
		current.current = connection;
		const set = next => {
			if (current.current === connection) {
				setState(next);
			}
		};

		set('connecting');
		connection.onreconnecting(() => set('reconnecting'));
		connection.onreconnected(() => set('live'));
		connection.onclose(() => set('offline'));

		return () => connection.start().then(
			() => set('live'),
			error => {
				set('offline');
				throw error;
			});
	}, []);

	return [state, track];
}
