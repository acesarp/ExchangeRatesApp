SELECT cb.Id, cb.BankCode, cb.BankName, nc.CurrencyCode AS NativeCurrency,
	   sc.CurrencyCode AS SupportedCurrency
FROM CentralBank cb
JOIN Currency nc ON nc.Id = cb.CurrencyId
LEFT JOIN CentralBankSupportedCurrency cbsc ON cbsc.CentralBankId = cb.Id
LEFT JOIN Currency sc ON sc.Id = cbsc.CurrencyId
WHERE nc.CurrencyCode IN ('AOA', 'MGA', 'MDL', 'MAD')
   OR sc.CurrencyCode IN ('AOA', 'MGA', 'MDL', 'MAD')
ORDER BY cb.BankCode, sc.CurrencyCode;

SELECT *
FROM CentralBank cb
JOIN Currency nc ON nc.Id = cb.CurrencyId

WHERE nc.CurrencyCode = 'AOA';

update CentralBank
set ApiUrl = 'https://www.bna.ao/service/rest/taxas/get/evolucao/taxa/intervalo'
where BankCode = 'BNA';