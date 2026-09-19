SELECT TOP (1000) [CentralBankId]
      ,[CurrencyId]
  FROM [ExchangeRates].[dbo].[CentralBankSupportedCurrency]

  SELECT DISTINCT s.CurrencyCode AS MissingCurrencyCode
FROM @Supported s
LEFT JOIN dbo.Currency c ON c.CurrencyCode = s.CurrencyCode
WHERE c.Id IS NULL
ORDER BY s.CurrencyCode;