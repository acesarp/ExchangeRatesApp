using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server;

public sealed record ExchangeRate(DateOnly Date, ECurrencyISO BaseCurrency, ECurrencyISO QuoteCurrency, decimal Rate, string Provider);



