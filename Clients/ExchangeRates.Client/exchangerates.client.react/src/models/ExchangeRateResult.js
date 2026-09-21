export default class ExchangeRateResult {
    constructor({ date, baseCurrency, quoteCurrency, rate, provider }) {
        this.date = String(date);
        this.baseCurrency = String(baseCurrency);
        this.quoteCurrency = String(quoteCurrency);
        this.rate = Number(rate);
        this.provider = String(provider);
    }
}