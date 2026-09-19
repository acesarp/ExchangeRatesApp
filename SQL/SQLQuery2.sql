INSERT INTO CentralBankSupportedCurrency (CentralBankId, CurrencyId)
SELECT cb.Id, c.Id
FROM CentralBank cb
CROSS JOIN Currency c
WHERE cb.BankCode = 'FRED'
  AND c.CurrencyCode = 'EUR'
  AND NOT EXISTS (
	  SELECT 1
	  FROM CentralBankSupportedCurrency x
	  WHERE x.CentralBankId = cb.Id
	    AND x.CurrencyId = c.Id
  );

INSERT INTO CentralBankSupportedCurrency (CentralBankId, CurrencyId)
SELECT cb.Id, c.Id
FROM CentralBank cb
CROSS JOIN Currency c
WHERE cb.BankCode = 'ECB'
  AND c.CurrencyCode = 'USD'
  AND NOT EXISTS (
	  SELECT 1
	  FROM CentralBankSupportedCurrency x
	  WHERE x.CentralBankId = cb.Id
	    AND x.CurrencyId = c.Id
  );