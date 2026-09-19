UPDATE cbsc
SET ProviderSeriesId =
    CASE c.CurrencyCode
        WHEN 'USD' THEN 'SF43718'
        WHEN 'EUR' THEN 'SF46410'
        WHEN 'JPY' THEN 'SF46406'
        WHEN 'GBP' THEN 'SF46407'
        WHEN 'CAD' THEN 'SF60632'
    END
FROM dbo.CentralBankSupportedCurrency cbsc
INNER JOIN dbo.CentralBank cb
    ON cb.Id = cbsc.CentralBankId
INNER JOIN dbo.Currency c
    ON c.Id = cbsc.CurrencyId
WHERE cb.BankCode = 'BANXICO'
  AND c.CurrencyCode IN ('USD', 'EUR', 'JPY', 'GBP', 'CAD');
  
  
  
  
  SELECT TOP (1000) [CentralBankId]
      ,[CurrencyId]
      ,[ProviderSeriesId]
  FROM [ExchangeRates].[dbo].[CentralBankSupportedCurrency]
  where CentralBankId = 1