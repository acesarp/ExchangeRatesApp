namespace ExchangeRates.Server;

public sealed record ExchangeRate(DateOnly Date, string BaseCurrency, string QuoteCurrency, decimal Rate, string Provider);



