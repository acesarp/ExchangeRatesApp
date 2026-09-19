
IF OBJECT_ID('dbo.ExchangeRateUnavailableDate', 'U') IS NULL 
BEGIN 
CREATE TABLE dbo .ExchangeRateUnavailableDate ( Id bigint IDENTITY (1,1 ) NOT NULL PRIMARY KEY, 
CentralBankId int NOT NULL, BaseCurrency char(3) NOT NULL, QuoteCurrency char(3) NOT NULL, 
UnavailableDate date NOT NULL, 
CreatedAtUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME ()
) 
END
CREATE UNIQUE INDEX UX_ExchangeRateUnavailableDate 
	ON dbo.ExchangeRateUnavailableDate ( CentralBankId, BaseCurrency, QuoteCurrency, UnavailableDate )