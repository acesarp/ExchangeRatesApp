using ExchangeRates.Domain.Enums;

using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central del Uruguay
/// </summary>
public sealed class BCUProvider : CentralBankProviderBase {
	private readonly ILogger<BCUProvider> _logger;

	public BCUProvider(HttpClient http, IConfiguration configuration, ILogger<BCUProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BCU";

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		if (quoteCurrency != ECurrencyISO.USD) {
			return [];
		}

		var results = new List<ExchangeRateResult>();

		for (var date = fromDate; date <= toDate; date = date.AddDays(1)) {
			try {
				var soapEnvelope = $"""
					<?xml version="1.0" encoding="utf-8"?>
					<soap:Envelope xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
						<soap:Body>
							<wsawsbcuC xmlns="Cotizaciones">
								<Entrada>
									<Moneda><item>0</item></Moneda>
									<FechaDesde>{date:dd/MM/yyyy}</FechaDesde>
									<FechaHasta>{date:dd/MM/yyyy}</FechaHasta>
									<Grupo>0</Grupo>
								</Entrada>
							</wsawsbcuC>
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

				foreach (var item in document.Descendants().Where(e => e.Name.LocalName.Equals("datoscotizaciones", StringComparison.OrdinalIgnoreCase))) {
					var nombre = item.Elements().FirstOrDefault(e => e.Name.LocalName.Equals("Nombre", StringComparison.OrdinalIgnoreCase))?.Value;

					if (nombre is null || !nombre.Contains(quoteCurrency.ToString(), StringComparison.OrdinalIgnoreCase)) {
						continue;
					}

					var tcvValue = item.Elements().FirstOrDefault(e => e.Name.LocalName.Equals("TCV", StringComparison.OrdinalIgnoreCase))?.Value;

					if (tcvValue is null || !decimal.TryParse(tcvValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
						continue;
					}

					if (InverseProvider) {
						rate = 1m / rate;
					}
					results.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
				}
			}
			catch (Exception ex) {
				_logger.LogWarning(ex, "Failed to fetch BCU rate for {Date}", date);
			}
		}

		return results;
	}
}
