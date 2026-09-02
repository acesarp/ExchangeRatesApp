import { useEffect, useMemo, useState } from 'react'
import { getAvailableCurrencies, getExchangeRates } from './services/currencyService'
import './App.css'

const CHART_WIDTH = 640
const CHART_HEIGHT = 220
const CHART_PADDING = 32

function RateChart({ rates }) {
  const points = useMemo(() => {
    if (!rates || rates.length === 0) return null

    const values = rates.map((r) => r.rate)
    const minValue = Math.min(...values)
    const maxValue = Math.max(...values)
    const range = maxValue - minValue || 1
    const stepX = rates.length > 1 ? (CHART_WIDTH - CHART_PADDING * 2) / (rates.length - 1) : 0

    return rates.map((r, index) => {
      const x = CHART_PADDING + index * stepX
      const y =
        CHART_HEIGHT - CHART_PADDING - ((r.rate - minValue) / range) * (CHART_HEIGHT - CHART_PADDING * 2)
      return { x, y, rate: r.rate, date: r.date }
    })
  }, [rates])

  if (!points) return null

  const linePath = points.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x} ${p.y}`).join(' ')

  return (
    <div className="chart-card">
      <h3 className="chart-title">Rate Trend</h3>
      <svg
        className="chart-svg"
        viewBox={`0 0 ${CHART_WIDTH} ${CHART_HEIGHT}`}
        role="img"
        aria-label="Exchange rate trend chart"
      >
        <line
          x1={CHART_PADDING}
          y1={CHART_HEIGHT - CHART_PADDING}
          x2={CHART_WIDTH - CHART_PADDING}
          y2={CHART_HEIGHT - CHART_PADDING}
          className="chart-axis"
        />
        <line
          x1={CHART_PADDING}
          y1={CHART_PADDING}
          x2={CHART_PADDING}
          y2={CHART_HEIGHT - CHART_PADDING}
          className="chart-axis"
        />
        <path d={linePath} className="chart-line" fill="none" />
        {points.map((p) => (
          <circle key={p.date} cx={p.x} cy={p.y} r="3" className="chart-point">
            <title>{`${p.date}: ${p.rate}`}</title>
          </circle>
        ))}
      </svg>
    </div>
  )
}

function getTodayIsoDate() {
  const now = new Date()
  const year = now.getFullYear()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function App() {
  const [currencies, setCurrencies] = useState([])
  const [baseCurrency, setBaseCurrency] = useState('')
  const [quoteCurrency, setQuoteCurrency] = useState('')
  const [fromDate, setFromDate] = useState(getTodayIsoDate)
  const [toDate, setToDate] = useState(getTodayIsoDate)

  const [rates, setRates] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [hasSearched, setHasSearched] = useState(false)

  useEffect(() => {
    const controller = new AbortController()

    async function loadCurrencies() {
      try {
        const data = await getAvailableCurrencies(controller.signal)
        setCurrencies(data ?? [])
      } catch (err) {
          if (err.name !== 'AbortError') {
              setError('Unable to load available currencies.' + err.name + ': ' + err.message)
        }
      }
    }

    loadCurrencies()
    return () => controller.abort()
  }, [])

  const canSubmit =
    Boolean(baseCurrency) &&
    Boolean(quoteCurrency) &&
    Boolean(fromDate) &&
    Boolean(toDate) &&
    baseCurrency !== quoteCurrency &&
    !loading

  async function handleSubmit(event) {
    event.preventDefault()
    if (!canSubmit) return

    setLoading(true)
    setError('')

    try {
      const data = await getExchangeRates({ baseCurrency, quoteCurrency, fromDate, toDate })
      setRates(data ?? [])
      setHasSearched(true)
    } catch {
      setError('Unable to fetch exchange rates. Please try again.')
      setRates([])
      setHasSearched(true)
    } finally {
      setLoading(false)
    }
  }

  const summary = useMemo(() => {
    if (!rates || rates.length === 0) return null
    const latest = rates[rates.length - 1]
    return {
      pair: `${baseCurrency} / ${quoteCurrency}`,
      range: `${fromDate} to ${toDate}`,
      count: rates.length,
      latestRate: latest.rate,
      latestDate: latest.date,
    }
  }, [rates, baseCurrency, quoteCurrency, fromDate, toDate])

  return (
    <div className="page">
      <header className="app-header">
        <h1>Exchange Rates</h1>
        <p className="subtitle">Look up historical currency exchange rates between two currencies.</p>
      </header>

      <main className="content">
        <section className="card form-card" aria-label="Exchange rate search">
          <form onSubmit={handleSubmit} noValidate>
            <div className="form-grid">
              <div className="form-field">
                <label htmlFor="baseCurrency">Base Currency</label>
                <select
                  id="baseCurrency"
                  value={baseCurrency}
                  onChange={(e) => setBaseCurrency(e.target.value)}
                  required
                >
                  <option value="" disabled>
                    Select currency
                  </option>
                  {currencies.map((currency) => (
                    <option key={currency} value={currency}>
                      {currency}
                    </option>
                  ))}
                </select>
              </div>

              <div className="form-field">
                <label htmlFor="quoteCurrency">Quote Currency</label>
                <select
                  id="quoteCurrency"
                  value={quoteCurrency}
                  onChange={(e) => setQuoteCurrency(e.target.value)}
                  required
                >
                  <option value="" disabled>
                    Select currency
                  </option>
                  {currencies.map((currency) => (
                    <option key={currency} value={currency}>
                      {currency}
                    </option>
                  ))}
                </select>
              </div>

              <div className="form-field">
                <label htmlFor="fromDate">From Date</label>
                <input
                  id="fromDate"
                  type="date"
                  value={fromDate}
                  onChange={(e) => setFromDate(e.target.value)}
                  required
                />
              </div>

              <div className="form-field">
                <label htmlFor="toDate">To Date</label>
                <input
                  id="toDate"
                  type="date"
                  value={toDate}
                  onChange={(e) => setToDate(e.target.value)}
                  required
                />
              </div>
            </div>

            {baseCurrency && quoteCurrency && baseCurrency === quoteCurrency && (
              <p className="field-error" role="alert">
                Base and quote currencies must be different.
              </p>
            )}

            <div className="form-actions">
              <button type="submit" className="btn btn-primary" disabled={!canSubmit}>
                {loading ? 'Loading…' : 'Get Rates'}
              </button>
            </div>
          </form>
        </section>

        {error && (
          <div className="alert alert-error" role="alert">
            {error}
          </div>
        )}

        {loading && (
          <div className="alert alert-loading" role="status">
            Fetching exchange rates…
          </div>
        )}

        {!loading && hasSearched && !error && (
          <section className="card results-card" aria-label="Exchange rate results">
            {summary && (
              <div className="summary-bar">
                <div className="summary-item">
                  <span className="summary-label">Pair</span>
                  <span className="summary-value">{summary.pair}</span>
                </div>
                <div className="summary-item">
                  <span className="summary-label">Date range</span>
                  <span className="summary-value">{summary.range}</span>
                </div>
                <div className="summary-item">
                  <span className="summary-label">Results</span>
                  <span className="summary-value">{summary.count}</span>
                </div>
                <div className="summary-item">
                  <span className="summary-label">Latest rate ({summary.latestDate})</span>
                  <span className="summary-value summary-highlight">{summary.latestRate}</span>
                </div>
              </div>
            )}

            {rates.length > 0 && <RateChart rates={rates} />}

            <div className="table-wrapper">
              <table className="rates-table">
                <thead>
                  <tr>
                    <th scope="col">Date</th>
                    <th scope="col">Base</th>
                    <th scope="col">Quote</th>
                    <th scope="col" className="numeric">
                      Rate
                    </th>
                    <th scope="col">Provider</th>
                  </tr>
                </thead>
                <tbody>
                  {rates.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="empty-state">
                        No rates found for the selected criteria.
                      </td>
                    </tr>
                  ) : (
                    rates.map((rate) => (
                      <tr key={`${rate.date}-${rate.baseCurrency}-${rate.quoteCurrency}`}>
                        <td>{rate.date}</td>
                        <td>{rate.baseCurrency}</td>
                        <td>{rate.quoteCurrency}</td>
                        <td className="numeric">{rate.rate}</td>
                        <td>{rate.provider}</td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </section>
        )}
      </main>
    </div>
  )
}

export default App
