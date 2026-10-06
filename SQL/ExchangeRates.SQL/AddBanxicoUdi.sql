-- Lists MXV (Mexican Unidad de Inversion, UDI) as supported by Banco de Mexico (BANXICO).
--
-- BANXICOProvider reads each currency from the SIE series in ProviderSeriesId. SP68257 is the daily UDI value in pesos
-- per UDI, quoted like the other series (e.g. SF43718 = pesos per USD); checked against the API on 2026-10-06
-- (2026-09-25: 8.824423 MXN per UDI). Banxico publishes it for every calendar day, weekends included.
-- Safe to re-run: only inserts the link when it does not exist yet.

SET QUOTED_IDENTIFIER ON;

INSERT INTO CentralBankSupportedCurrency (CentralBankId, CurrencyId, ProviderSeriesId)
SELECT b.Id, c.Id, 'SP68257'
FROM CentralBank b
CROSS JOIN Currency c
WHERE b.BankCode = 'BANXICO'
	AND c.CurrencyCode = 'MXV'
	AND NOT EXISTS (
		SELECT 1 FROM CentralBankSupportedCurrency s
		WHERE s.CentralBankId = b.Id AND s.CurrencyId = c.Id
	);

SELECT b.BankCode, c.CurrencyCode, s.ProviderSeriesId
FROM CentralBankSupportedCurrency s
JOIN CentralBank b ON b.Id = s.CentralBankId
JOIN Currency c ON c.Id = s.CurrencyId
WHERE b.BankCode = 'BANXICO' AND c.CurrencyCode = 'MXV';
