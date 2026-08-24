using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central de Costa Rica
/// </summary>
public sealed class BCCRProvider : CentralBankProviderBase {
	public BCCRProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) {
		Token = configuration["CentralBanks:BCCR:Token"] ?? throw new InvalidOperationException("BCCR Token is not configured.");
		NameParameter = configuration["CentralBanks:BCCR:UserName"] ?? throw new InvalidOperationException("BCCR UserName is not configured.");
		Email = configuration["CentralBanks:BCCR:Email"] ?? throw new InvalidOperationException("BCCR Email is not configured.");
	}
	private string Token { get; }
	private string NameParameter { get; }
	private string Email { get; }

	public override string Code => "BCCR";
	public override string Name => "Banco Central de Costa Rica";
	public override ECurrency NativeCurrency => ECurrency.CRC;
	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrency fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		const int indicator = 318; // USD reference selling rate

		var url = $"{Url.TrimEnd('/')}/ObtenerIndicadoresEconomicosXML" +
			$"?Indicador={indicator}" +
			$"&FechaInicio={fromDate:dd/MM/yyyy}" +
			$"&FechaFinal={toDate:dd/MM/yyyy}" +
			$"&Nombre={Uri.EscapeDataString(NameParameter)}" +
			$"&SubNiveles=N" +
			$"&CorreoElectronico={Uri.EscapeDataString(Email)}" +
			$"&Token={Uri.EscapeDataString(Token)}";

		var xml = await Http.GetStringAsync(url, ct);
		var document = XDocument.Parse(xml);

		var rates = new List<ExchangeRate>();

		foreach (var row in document.Descendants().Where(x => x.Name.LocalName == "INGC011_CAT_INDICADORECONOMIC")) {
			var dateText = row.Elements().FirstOrDefault(x => x.Name.LocalName == "DES_FECHA")?.Value;
			var valueText = row.Elements().FirstOrDefault(x => x.Name.LocalName == "NUM_VALOR")?.Value;

			if (!DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
				continue;
			}

			if (!decimal.TryParse(valueText, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
				continue;
			}

			rates.Add(new ExchangeRate(DateOnly.FromDateTime(date), fromCurrency, NativeCurrency, rate, Code));
		}

		return rates;
	}
}
