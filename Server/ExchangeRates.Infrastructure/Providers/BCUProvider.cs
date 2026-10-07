
using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Globalization;
using System.Text;
using System.Xml.Linq;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Banco Central del Uruguay
/// </summary>
public sealed class BCUProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<BCUProvider> _logger;

	public BCUProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BCUProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		// BCU identifies currencies by its own numeric codes (ProviderSeriesId), all quoted in pesos per unit:
		// 2225 = US dollar (billete), 9800 = Unidad Indexada (UYI), 9700 = Unidad Previsional (UYW).
		var bcuCode = Bank.SupportedCurrencies.FirstOrDefault(s => s.Currency.CurrencyCode == quoteCurrency)?.ProviderSeriesId
			?? (quoteCurrency == "USD" ? "2225" : null);

		if (string.IsNullOrWhiteSpace(bcuCode)) {
			return [];
		}

		var results = new List<ExchangeRateResult>();

		try {
			var soapEnvelope = $"""
				<?xml version="1.0" encoding="utf-8"?>
				<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/" xmlns:cot="Cotiza">
					<soap:Body>
						<cot:wsbcucotizaciones.Execute>
							<cot:Entrada>
								<cot:Moneda><cot:item>{bcuCode}</cot:item></cot:Moneda>
								<cot:FechaDesde>{fromDate:yyyy-MM-dd}</cot:FechaDesde>
								<cot:FechaHasta>{toDate:yyyy-MM-dd}</cot:FechaHasta>
								<cot:Grupo>0</cot:Grupo>
							</cot:Entrada>
						</cot:wsbcucotizaciones.Execute>
					</soap:Body>
				</soap:Envelope>
				""";

			using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl) {
				Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml")
			};
			request.Headers.TryAddWithoutValidation("SOAPAction", "\"Cotizaaction/AWSBCUCOTIZACIONES.Execute\"");
			using var response = await Http.SendAsync(request, ct);
			response.EnsureSuccessStatusCode();

			var document = XDocument.Parse(await response.Content.ReadAsStringAsync(ct));

			// <datoscotizaciones.dato><Fecha>2026-09-29</Fecha><Moneda>2225</Moneda><TCV>40.237</TCV></datoscotizaciones.dato>
			foreach (var item in document.Descendants().Where(e => e.Name.LocalName.Equals("datoscotizaciones.dato", StringComparison.OrdinalIgnoreCase))) {
				string? Value(string name) => item.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value;

				if (!DateOnly.TryParse(Value("Fecha"), CultureInfo.InvariantCulture, out var date) || date < fromDate || date > toDate) {
					continue;
				}

				if (!decimal.TryParse(Value("TCV"), NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
			}
		}
		catch (Exception ex) when (ex is not OperationCanceledException) {
			_logger.LogWarning(ex, "Failed to fetch BCU rates for {FromDate}-{ToDate}", fromDate, toDate);
		}

		return results;
	}
}

