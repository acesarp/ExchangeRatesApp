 DECLARE @currency1 nvarchar(3) = N'BRL';
DECLARE @currency2 nvarchar(3) = N'EUR';

SELECT c.Id, c.BankCode, c.BankName, 
c.CountryOfOrigin, c.CreatedAtUtc, 
c.CurrencyId, 
c.IsActive, c.Priority
FROM CentralBank AS c
INNER JOIN Currency AS c0 ON c.CurrencyId = c0.Id
WHERE c.IsActive = CAST(1 AS bit) AND ((c0.CurrencyCode = @currency1 AND EXISTS (
    SELECT 1
    FROM CentralBankSupportedCurrency AS c1
    INNER JOIN Currency AS c2 ON c1.CurrencyId = c2.Id
    WHERE c.Id = c1.CentralBankId AND c2.CurrencyCode = @currency2)) OR (c0.CurrencyCode = @currency2 AND EXISTS (
    SELECT 1
    FROM CentralBankSupportedCurrency AS c3
    INNER JOIN Currency AS c4 ON c3.CurrencyId = c4.Id
    WHERE c.Id = c3.CentralBankId AND c4.CurrencyCode = @currency1)))
ORDER BY COALESCE(c.Priority, 2147483647), c.BankCode