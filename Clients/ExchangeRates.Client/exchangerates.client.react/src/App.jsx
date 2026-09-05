import { useEffect, useMemo, useRef, useState } from 'react';
import ChartJS from 'chart.js/auto'
import { getAvailableCurrencies, getExchangeRates, getZacaMedia, getEnvironment } from './services/currencyService';
import './App.css';


const PRIORITY_CURRENCIES = ['USD', 'EUR', 'BRL', 'CAD', 'GBP', 'AUD'];

function getTodayIsoDate() {
    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

function RateChart({ rates, baseCurrency, quoteCurrency, zacaPictureSrc }) {
    const canvasRef = useRef(null);
    const chartRef = useRef(null);
    const [hoverPosition, setHoverPosition] = useState(null);

    useEffect(() => {
        if (!canvasRef.current || !rates?.length) return;

        chartRef.current?.destroy();

        const firstRate = Number(rates[0].rate);
        const lastRate = Number(rates[rates.length - 1].rate);
        const trendColor = lastRate >= firstRate ? '#90ee90' : '#FF6666';

        chartRef.current = new ChartJS(canvasRef.current, {
            type: 'line',
            data: {
                labels: rates.map(r => r.date),
                datasets: [{
                    label: `${baseCurrency} / ${quoteCurrency}`,
                    data: rates.map(r => r.rate),
                    borderColor: trendColor,
                    backgroundColor: trendColor,
                    borderWidth: 2,
                    pointRadius: 3,
                    pointHoverRadius: 5,
                    tension: 0.2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: {
                    mode: 'index',
                    intersect: false
                },
                plugins: {
                    legend: {
                        display: true,
                        position: 'top'
                    },
                    tooltip: {
                        callbacks: {
                            label: context => `${baseCurrency} / ${quoteCurrency}: ${context.parsed.y}`
                        }
                    }
                },
                scales: {
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            maxRotation: 0,
                            autoSkip: true,
                            callback(value) {
                                const isoDate = this.getLabelForValue(value)
                                if (!isoDate) return ''

                                const [, month, day] = isoDate.split('-')
                                return `${day}-${month}`
                            }
                        }
                    },
                    y: {
                        ticks: {
                            callback: value => Number(value).toFixed(4)
                        }
                    }
                },
                onHover: event => {
                    if (event.x == null || event.y == null) return
                    setHoverPosition({ x: event.x, y: event.y })
                }
            }
        })

        return () => {
            chartRef.current?.destroy()
            chartRef.current = null
        }
    }, [rates, baseCurrency, quoteCurrency])

    if (!rates?.length) return null

    return (
        <div className="chart-card">
            <h3 className="chart-title">Rate Trend</h3>
            <div className="chart-container" onMouseLeave={() => setHoverPosition(null)}>
                <canvas ref={canvasRef} />

                {hoverPosition && zacaPictureSrc && (
                    <img src={zacaPictureSrc} alt="Zaca" className="chart-zaca-image" style={{ left: `${hoverPosition.x}px`, top: `${hoverPosition.y}px` }} />
                )}
            </div>
        </div>
    )
}

function App() {
    const [environment, setEnvironment] = useState('');
    const [currencies, setCurrencies] = useState([]);
    const [baseCurrency, setBaseCurrency] = useState('USD');
    const [quoteCurrency, setQuoteCurrency] = useState('EUR');
    const [fromDate, setFromDate] = useState(getTodayIsoDate);
    const [toDate, setToDate] = useState(getTodayIsoDate);

    const [rates, setRates] = useState([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');
    const [hasSearched, setHasSearched] = useState(false);

    const [zacaPictureSrc, setZacaPictureSrc] = useState('');
    const audioRef = useRef(null);

    useEffect(() => {
        const controller = new AbortController();
        getEnvironment()
            .then(data => setEnvironment(data.environment))
            .catch(() => setEnvironment('Unknown'));

        async function loadCurrencies() {
            try {
                const data = await getAvailableCurrencies(controller.signal);
                const availableCurrencies = data ?? [];
                setCurrencies(availableCurrencies);

                if (availableCurrencies.length > 0) {
                    setBaseCurrency(availableCurrencies.includes('USD') ? 'USD' : availableCurrencies[0]);
                    setQuoteCurrency(availableCurrencies.includes('EUR') ? 'EUR' : availableCurrencies[0]);
                }
            } catch (err) {
                if (err.name !== 'AbortError') {
                    setError('Unable to load available currencies.' + err.name + ': ' + err.message);
                }
            }
        }

        loadCurrencies();
        return () => controller.abort();
    }, []);

    const sortedCurrencies = useMemo(() => {
        return [...currencies].sort((a, b) => {
            const aPriority = PRIORITY_CURRENCIES.indexOf(a);
            const bPriority = PRIORITY_CURRENCIES.indexOf(b);

            if (aPriority !== -1 && bPriority !== -1) return aPriority - bPriority;
            if (aPriority !== -1) return -1;
            if (bPriority !== -1) return 1;
            return a.localeCompare(b);
        })
    }, [currencies]);

    const canSubmit =
        Boolean(baseCurrency) &&
        Boolean(quoteCurrency) &&
        Boolean(fromDate) &&
        Boolean(toDate) &&
        baseCurrency !== quoteCurrency &&
        !loading;

    async function handleSubmit(event) {
        event.preventDefault();
        if (!canSubmit) return;
        await loadRates(baseCurrency, quoteCurrency);
    }

    async function handleSwapCurrencies() {
        const newBase = quoteCurrency;
        const newQuote = baseCurrency;

        setBaseCurrency(newBase);
        setQuoteCurrency(newQuote);

        await loadRates(newBase, newQuote);
    }

    async function loadRates(base, quote) {
        if (!base || !quote || base === quote || !fromDate || !toDate || loading) return;

        setLoading(true);
        setError('');

        try {
            const data = await getExchangeRates({ baseCurrency: base, quoteCurrency: quote, fromDate, toDate });
            setRates(data ?? []);
            setHasSearched(true);

            if (data?.length > 0) {
                try {
                    const media = await getZacaMedia();

                    if (media?.picture) setZacaPictureSrc(`data:image/png;base64,${media.picture}`);

                    if (media?.audio && audioRef.current) {
                        audioRef.current.src = `data:audio/mpeg;base64,${media.audio}`;
                        audioRef.current.play().catch(() => { });
                    }
                } catch {
                    // ignore zaca media failures
                }
            }
        } catch {
            setError('Unable to fetch exchange rates. Please try again.');
            setRates([]);
            setHasSearched(true);
        } finally {
            setLoading(false);
        }
    }

    const summary = useMemo(() => {
        if (!rates || rates.length === 0) return null;
        const latest = rates[rates.length - 1];
        return {
            pair: `${baseCurrency} / ${quoteCurrency}`,
            range: `${fromDate} to ${toDate}`,
            count: rates.length,
            latestRate: latest.rate,
            latestDate: latest.date,
        }
    }, [rates, baseCurrency, quoteCurrency, fromDate, toDate]);

  return (
    <div className="page">
      <audio ref={audioRef} hidden />
      <header className="app-header">
        <h1>Exchange Rates</h1>
              <p className="subtitle">Look up historical currency exchange rates between two currencies.</p>
              <div className={`environment-badge environment-${environment.toLowerCase()}`}>{environment}</div>
      </header>

      <main className="content">
        <section className="card form-card" aria-label="Exchange rate search">
          <form onSubmit={handleSubmit} noValidate>
                      <div className="form-grid">
                          <div className="currency-row">
                              <div className="form-field currency-field">
                <label htmlFor="baseCurrency">Base Currency</label>
                <select id="baseCurrency" value={baseCurrency} onChange={(e) => setBaseCurrency(e.target.value)} required>
                  <option value="" disabled>Select currency</option>
                  {sortedCurrencies.map((currency) => (
                    <option key={currency} value={currency}>{currency}</option>
                  ))}
                </select>
              </div>
            <div className="currency-swap">
                <button type="button" className="swap-button" onClick={handleSwapCurrencies} disabled={!baseCurrency || !quoteCurrency || loading} title="Swap currencies" aria-label="Swap base and quote currencies">
                    ⇄
                </button>
            </div>
              <div className="form-field currency-field">
                <label htmlFor="quoteCurrency">Quote Currency</label>
                <select id="quoteCurrency" value={quoteCurrency} onChange={(e) => setQuoteCurrency(e.target.value)} required>
                  <option value="" disabled>Select currency</option>
                  {sortedCurrencies.map((currency) => (
                    <option key={currency} value={currency}>{currency}</option>
                  ))}
                </select>
              </div>
            </div>

              <div className="form-field">
                <label htmlFor="fromDate">From Date</label>
                <input id="fromDate" type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} required />
              </div>

              <div className="form-field">
                <label htmlFor="toDate">To Date</label>
                <input id="toDate" type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} required />
              </div>
            </div>

            {baseCurrency && quoteCurrency && baseCurrency === quoteCurrency && (
              <p className="field-error" role="alert">Base and quote currencies must be different.</p>
            )}

            <div className="form-actions">
              <button type="submit" className="btn btn-primary" disabled={!canSubmit}> {loading ? 'Loading…' : 'Get Rates'} </button>
            </div>
          </form>
        </section>

        {error && (
          <div className="alert alert-error" role="alert">{error}</div>
        )}

        {loading && (
          <div className="alert alert-loading" role="status">Fetching exchange rates…</div>
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

            {rates.length > 0 && (
              <RateChart rates={rates} baseCurrency={baseCurrency} quoteCurrency={quoteCurrency} zacaPictureSrc={zacaPictureSrc}     />
            )}

            <div className="table-wrapper">
              <table className="rates-table">
                <thead>
                  <tr>
                    <th scope="col">Date</th>
                    <th scope="col">Base</th>
                    <th scope="col">Quote</th>
                    <th scope="col" className="numeric">Rate</th>
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

export default App;
