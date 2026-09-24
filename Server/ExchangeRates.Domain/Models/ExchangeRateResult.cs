namespace ExchangeRates.Domain;

public sealed record ExchangeRateResult(DateOnly Date, string BaseCurrency, string QuoteCurrency, decimal Rate, string Provider);



