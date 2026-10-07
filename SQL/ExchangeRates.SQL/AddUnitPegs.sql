-- Adds fixed pegs for ISO codes that no central bank publishes but that are defined 1:1 against a traded currency.
-- The service falls back to FixedExchangeRate when no bank quotes a currency (1 PeggedOn = Rate units of Currency).
--
--   USN -> USD  US dollar "next day" funds
--   CHE -> EUR  WIR euro, exchanged 1:1 by WIR Bank
--   CHW -> CHF  WIR franc, exchanged 1:1 by WIR Bank
--   VED -> VES  digital bolivar (2021). Sources still quote the post-2021 bolivar under VES (FRED USD/VES ~ 850 in
--               2026), so both codes refer to the same unit.
--   XUA -> XDR  ADB unit of account, equal to the IMF SDR. Only resolves once an XDR source works (IMF provider).
--   BOV -> USD  Bolivian Mvdol: bolivianos with value maintained against the US dollar.
--
-- Safe to re-run: only inserts pegs that do not exist yet.

SET QUOTED_IDENTIFIER ON;

INSERT INTO FixedExchangeRate (CurrencyId, PeggedOnCurrencyId, Rate, IsActive)
SELECT c.Id, p.Id, 1, 1
FROM (VALUES ('USN', 'USD'), ('CHE', 'EUR'), ('CHW', 'CHF'), ('VED', 'VES'), ('XUA', 'XDR'), ('BOV', 'USD')) AS pegs(Currency, PeggedOn)
JOIN Currency c ON c.CurrencyCode = pegs.Currency
JOIN Currency p ON p.CurrencyCode = pegs.PeggedOn
WHERE NOT EXISTS (SELECT 1 FROM FixedExchangeRate f WHERE f.CurrencyId = c.Id AND f.PeggedOnCurrencyId = p.Id);

SELECT c.CurrencyCode AS Currency, p.CurrencyCode AS PeggedOn, f.Rate, f.IsActive
FROM FixedExchangeRate f
JOIN Currency c ON c.Id = f.CurrencyId
JOIN Currency p ON p.Id = f.PeggedOnCurrencyId
WHERE c.CurrencyCode IN ('USN', 'CHE', 'CHW', 'VED', 'XUA', 'BOV');
