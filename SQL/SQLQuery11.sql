SELECT TOP (1000) [Id]
      ,[CentralBankId]
      ,[BaseCurrency]
      ,[QuoteCurrency]
      ,[UnavailableDate]
      ,[CreatedAtUtc]
      ,[Reason]
  FROM [ExchangeRates].[dbo].[ExchangeRateUnavailableDate]
  order by [CreatedAtUtc] desc