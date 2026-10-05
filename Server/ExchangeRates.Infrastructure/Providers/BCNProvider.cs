
using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Globalization;
using System.Text;
using System.Xml.Linq;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Banco Central de Nicaragua
/// </summary>
public sealed class BCNProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<BCNProvider> _logger;

	public BCNProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BCNProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		if (quoteCurrency != "USD") {
			return [];
		}

		var results = new List<ExchangeRateResult>();

		// RecuperaTC_Dia(Ano, Mes, Dia) returns the official NIO/USD rate for one calendar day.
		for (var date = fromDate; date <= toDate; date = date.AddDays(1)) {
			try {
				var soapEnvelope = $"""
					<?xml version="1.0" encoding="utf-8"?>
					<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
						<soap:Body>
							<RecuperaTC_Dia xmlns="http://servicios.bcn.gob.ni/">
								<Ano>{date.Year}</Ano>
								<Mes>{date.Month}</Mes>
								<Dia>{date.Day}</Dia>
							</RecuperaTC_Dia>
						</soap:Body>
					</soap:Envelope>
					""";

				using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl) {
					Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml")
				};
				request.Headers.TryAddWithoutValidation("SOAPAction", "\"http://servicios.bcn.gob.ni/RecuperaTC_Dia\"");
				using var response = await Http.SendAsync(request, ct);

				if (!response.IsSuccessStatusCode) {
					continue;
				}

				var document = XDocument.Parse(await response.Content.ReadAsStringAsync(ct));
				var value = document.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("RecuperaTC_DiaResult", StringComparison.OrdinalIgnoreCase))?.Value;

				if (value is null || !decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
			}
			catch (Exception ex) when (ex is not OperationCanceledException) {
				_logger.LogWarning(ex, "Failed to fetch BCN rate for {Date}", date);
			}
		}

		return results;
	}
}

