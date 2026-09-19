SELECT TOP (1000) *
  FROM [ExchangeRates].[dbo].[ExchangeRate] as r
  
  join ExchangeRates.dbo.Currency as c on c.NumericCode = r.BaseCurrencyId