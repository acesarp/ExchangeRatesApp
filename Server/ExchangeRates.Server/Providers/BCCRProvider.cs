using ExchangeRates.Domain.Enums;

using System.Globalization;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central de Costa Rica
/// </summary>
public sealed class BCCRProvider : CentralBankProviderBase {
	private readonly ILogger<BCCRProvider> _logger;
	public BCCRProvider(HttpClient http, IConfiguration configuration, ILogger<BCCRProvider> logger) : base(http, configuration) {
		_logger = logger;
	}
	private string Token { get; set; }
	private string NameParameter { get; set; }
	private string Email { get; set; }

	public override string Code => "BCCR";
	public override string Name => "Banco Central de Costa Rica";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.CRC;
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		Token = Configuration["CentralBanks:BCCR:Token"] ?? throw new InvalidOperationException("BCCR Token is not configured.");
		NameParameter = Configuration["CentralBanks:BCCR:UserName"] ?? throw new InvalidOperationException("BCCR UserName is not configured.");
		Email = Configuration["CentralBanks:BCCR:Email"] ?? throw new InvalidOperationException("BCCR Email is not configured.");
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

		var rates = new List<ExchangeRateResult>();

		foreach (var row in document.Descendants().Where(x => x.Name.LocalName == "INGC011_CAT_INDICADORECONOMIC")) {
			var dateText = row.Elements().FirstOrDefault(x => x.Name.LocalName == "DES_FECHA")?.Value;
			var valueText = row.Elements().FirstOrDefault(x => x.Name.LocalName == "NUM_VALOR")?.Value;

			if (!DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
				continue;
			}

			if (!decimal.TryParse(valueText, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
				continue;
			}

			rates.Add(new ExchangeRateResult(DateOnly.FromDateTime(date), quoteCurrency, NativeCurrency, rate, Code));
		}

		return rates;
	}
}
