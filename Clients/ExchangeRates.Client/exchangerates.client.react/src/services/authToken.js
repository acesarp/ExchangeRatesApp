import { loginRequest } from './authConfig.js';

/**
 * Returns an access token for the signed-in user, or undefined when nobody is signed in or the token
 * cannot be acquired silently. The hubs accept anonymous connections, so callers just connect without it.
 * Reads the account from the instance at call time, so it also works after a login that happened later.
 */
export async function getAccessToken(instance) {
	const account = instance.getActiveAccount() ?? instance.getAllAccounts()[0];
	if (!account) {
		return undefined;
	}

	try {
		const response = await instance.acquireTokenSilent({ ...loginRequest, account });
		return response.accessToken;
	} catch (error) {
		console.warn('Could not acquire access token silently; connecting anonymously.', error);
		return undefined;
	}
}
