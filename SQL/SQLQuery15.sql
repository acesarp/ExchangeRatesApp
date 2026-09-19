SELECT TOP (1000) [Id]
      ,[BankCode]
      ,[BankName]
      ,[CurrencyId]
      ,[IsActive]
      ,[CreatedAtUtc]
      ,[Priority]
      ,[CountryOfOrigin]
      ,[ApiUrl]
  FROM [ExchangeRates].[dbo].[CentralBank]


  ALTER TABLE dbo.CentralBankSupportedCurrency
ADD ProviderSeriesId NVARCHAR(50) NULL;