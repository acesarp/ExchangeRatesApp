export default class CurrencyModel {
    constructor(currencyCode, numericCode, name, isHistoric) {
        this.currencyCode = String(currencyCode);
        this.numericCode = Number(numericCode);
        this.name = String(name);
        this.isHistoric = Boolean(isHistoric);
    }
}