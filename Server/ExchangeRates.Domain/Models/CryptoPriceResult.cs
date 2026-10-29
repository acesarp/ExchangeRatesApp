namespace ExchangeRates.Domain.Models;

public sealed record CryptoPriceResult(string Symbol, decimal Price, DateTime TimestampUtc, string Provider);