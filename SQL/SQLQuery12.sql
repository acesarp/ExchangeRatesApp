SELECT TOP (1000) *
      from [ExchangeRates].[dbo].[CentralBank] as c
  join [ExchangeRates].[dbo].[CentralBankSupportedCurrency] as cbsc
  on c.Id = cbsc.CentralBankId
  join [ExchangeRates].[dbo].[Currency] as cur 
  on cbsc.CurrencyId = cur.Id
  where BankCode = 'BANXICO'

