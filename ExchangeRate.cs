using ExchangeRates.Server.Enums;

namespace ExchangeRates.Server;

public sealed record ExchangeRate(DateOnly date, ECurrency BaseCurrency, ECurrency QuoteCurrency, decimal Rate, string Provider);



