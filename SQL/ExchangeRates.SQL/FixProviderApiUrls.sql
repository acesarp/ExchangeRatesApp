-- Corrects CentralBank.ApiUrl values that point at endpoints which no longer serve exchange rates.
-- Safe to re-run: each statement only updates the row when it still holds the old value.

-- Required by the filtered index on CentralBank.Priority (sqlcmd defaults this to OFF).
SET QUOTED_IDENTIFIER ON;

-- TCMB: evds2 now serves an HTML app; the daily XML bulletins live under /kurlar/{yyyyMM}/{ddMMyyyy}.xml
UPDATE CentralBank
SET ApiUrl = 'https://www.tcmb.gov.tr/kurlar'
WHERE BankCode = 'TCMB' AND ApiUrl = 'https://evds2.tcmb.gov.tr/service/evds/';

-- MNB: the SOAP service only accepts POSTs on the http address published in its WSDL (https returns 404).
UPDATE CentralBank
SET ApiUrl = 'http://www.mnb.hu/arfolyamok.asmx'
WHERE BankCode = 'MNB' AND ApiUrl = 'https://www.mnb.hu/arfolyamok.asmx';
