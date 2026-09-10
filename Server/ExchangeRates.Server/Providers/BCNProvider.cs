
using System.Globalization;
using System.Text;
using System.Xml.Linq;

using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central de Nicaragua
/// </summary>
public sealed class BCNProvider : CentralBankProviderBase {
	private readonly ILogger<BCNProvider> _logger;

	public BCNProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BCNProvider> logger) : base(http, configuration, bank) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		if (quoteCurrency != "USD") {
			return [];
		}

		var results = new List<ExchangeRateResult>();

		for (var date = fromDate; date <= toDate; date = date.AddDays(1)) {
			try {
				var soapEnvelope = $"""
					<?xml version="1.0" encoding="utf-8"?>
					<soap:Envelope xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
						<soap:Body>
							<obtenerTipoCambio xmlns="http://tempuri.org/">
								<Fecha>{date:yyyy-MM-dd}</Fecha>
							</obtenerTipoCambio>
						</soap:Body>
					</soap:Envelope>
					""";

				using var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
				using var response = await Http.PostAsync(Url, content, ct);

				if (!response.IsSuccessStatusCode) {
					continue;
				}

				var xml = await response.Content.ReadAsStringAsync(ct);
				var document = XDocument.Parse(xml);
				var value = document.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("obtenerTipoCambioResult", StringComparison.OrdinalIgnoreCase))?.Value;

				if (value is null || !decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
					continue;
				}



				results.Add(new ExchangeRateResult(date, Bank.Currency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
			}
			catch (Exception ex) {
				_logger.LogWarning(ex, "Failed to fetch BCN rate for {Date}", date);
			}
		}

		return results;
	}
}

