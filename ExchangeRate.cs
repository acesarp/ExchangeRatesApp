using ExchangeRates.Server.Enums;

namespace ExchangeRates.Server;

public sealed record ExchangeRate(DateOnly date, ECurrencyISO BaseCurrency, ECurrencyISO QuoteCurrency, decimal Rate, string Provider);



