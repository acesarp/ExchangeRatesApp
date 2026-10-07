-- Lists UYI (Unidad Indexada) and UYW (Unidad Previsional) as supported by Banco Central del Uruguay (BCU).
--
-- BCUProvider reads each currency's BCU code from ProviderSeriesId (USD falls back to 2225). The BCU quotation service
-- publishes both units in pesos per unit, every calendar day; checked against it on 2026-10-06
-- (2026-09-21: UI 6.6463 UYU, UP 1.8062 UYU).
-- Safe to re-run: only inserts the links that do not exist yet.

SET QUOTED_IDENTIFIER ON;

INSERT INTO CentralBankSupportedCurrency (CentralBankId, CurrencyId, ProviderSeriesId)
SELECT b.Id, c.Id, units.BcuCode
FROM (VALUES ('UYI', '9800'), ('UYW', '9700')) AS units(CurrencyCode, BcuCode)
JOIN Currency c ON c.CurrencyCode = units.CurrencyCode
CROSS JOIN CentralBank b
WHERE b.BankCode = 'BCU'
	AND NOT EXISTS (
		SELECT 1 FROM CentralBankSupportedCurrency s
		WHERE s.CentralBankId = b.Id AND s.CurrencyId = c.Id
	);

SELECT b.BankCode, c.CurrencyCode, s.ProviderSeriesId
FROM CentralBankSupportedCurrency s
JOIN CentralBank b ON b.Id = s.CentralBankId
JOIN Currency c ON c.Id = s.CurrencyId
WHERE b.BankCode = 'BCU' AND c.CurrencyCode IN ('UYI', 'UYW');
