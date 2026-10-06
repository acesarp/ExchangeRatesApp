-- Clears the cached exchange rates so they are re-fetched from the fixed providers.
--
-- Why: before the rate-direction fix, many providers returned "native per foreign" values where the service expects
-- "1 native = x foreign", and rates requested in the non-canonical direction were saved under swapped currencies.
-- ExchangeRate has no provider column, so the bad rows cannot be told apart from the good ones.
-- ExchangeRateUnavailableDate is cleared too: providers that returned partial data (e.g. only the first day of the
-- range) caused real business days to be recorded as unavailable, which stops them from ever being fetched again.
--
-- Both tables are copied to *_Backup_20261005 first. Drop the backups once the refreshed data has been checked:
--   DROP TABLE ExchangeRate_Backup_20261005;
--   DROP TABLE ExchangeRateUnavailableDate_Backup_20261005;
--
-- Not re-runnable as-is: SELECT INTO fails if the backup tables already exist, which stops a second run from
-- overwriting the first backup.

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

SELECT * INTO ExchangeRate_Backup_20261005 FROM ExchangeRate;
SELECT * INTO ExchangeRateUnavailableDate_Backup_20261005 FROM ExchangeRateUnavailableDate;

DELETE FROM ExchangeRate;
DELETE FROM ExchangeRateUnavailableDate;

COMMIT TRANSACTION;

SELECT
	(SELECT COUNT(*) FROM ExchangeRate_Backup_20261005) AS ExchangeRateBackedUp,
	(SELECT COUNT(*) FROM ExchangeRateUnavailableDate_Backup_20261005) AS UnavailableDatesBackedUp,
	(SELECT COUNT(*) FROM ExchangeRate) AS ExchangeRateRemaining,
	(SELECT COUNT(*) FROM ExchangeRateUnavailableDate) AS UnavailableDatesRemaining;
