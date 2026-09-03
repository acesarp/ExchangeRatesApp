const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7255'

async function handleResponse(response) {
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
  return response.json()
}

export async function getAvailableCurrencies(signal) {
	const response = await fetch(`${API_BASE_URL}/api/available-currencies`, { signal });

  return handleResponse(response)
}

export async function getExchangeRates({ baseCurrency, quoteCurrency, fromDate, toDate }, signal) {
  const params = new URLSearchParams({baseCurrency,	quoteCurrency,	fromDate,	toDate  })
	const response = await fetch(`${API_BASE_URL}/api/exchange-rates?${params.toString()}`, { signal })

  return handleResponse(response)
}

export async function getZacaMedia(signal) {
  const response = await fetch(`${API_BASE_URL}/api/zaca-media`, { signal })
  return handleResponse(response)
}
