-- Lists SVC, XCG and ZWG as supported by Banca d'Italia (BDI).
--
-- Why: BDI publishes daily EUR rates for all three (checked against its API on 2026-10-06), but they were missing
-- from CentralBankSupportedCurrency, so no bank offered them and they could not be converted at all.
-- Safe to re-run: only inserts the links that do not exist yet.

SET QUOTED_IDENTIFIER ON;

INSERT INTO CentralBankSupportedCurrency (CentralBankId, CurrencyId)
SELECT b.Id, c.Id
FROM CentralBank b
CROSS JOIN Currency c
WHERE b.BankCode = 'BDI'
	AND c.CurrencyCode IN ('SVC', 'XCG', 'ZWG')
	AND NOT EXISTS (
		SELECT 1 FROM CentralBankSupportedCurrency s
		WHERE s.CentralBankId = b.Id AND s.CurrencyId = c.Id
	);

SELECT c.CurrencyCode
FROM CentralBankSupportedCurrency s
JOIN CentralBank b ON b.Id = s.CentralBankId
JOIN Currency c ON c.Id = s.CurrencyId
WHERE b.BankCode = 'BDI' AND c.CurrencyCode IN ('SVC', 'XCG', 'ZWG');
