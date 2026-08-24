using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Monetary Authority of Macao
/// </summary>
public sealed class AMCMProvider : CentralBankProviderBase {
	public AMCMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "AMCM";
	public override string Name => "Monetary Authority of Macao";
	public override ECurrency NativeCurrency => ECurrency.MOP;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrency fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		var url = $"{Url}?QueryType=1&Begin={fromDate:yyyyMMdd}&End={toDate:yyyyMMdd}";

		using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, ct));
		var rates = new List<ExchangeRate>();

		if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array) {
			return rates;
		}

		foreach (var row in data.EnumerateArray()) {
			var code = row.TryGetProperty("currency", out var c) ? c.GetString() : null;
			if (string.IsNullOrWhiteSpace(code)) {
				continue;
			}

			var unit = GetDecimal(row, "unit");
			var value = GetDecimal(row, "usdMeanValue");
			if (unit <= 0 || value <= 0) {
				continue;
			}
			rates.Add(new ExchangeRate(fromDate, fromCurrency!, NativeCurrency, value / unit, Code));
		}
		return rates;
	}
}

/// <summary>
/// Bank Al-Maghrib
/// </summary>
public sealed class BAMProvider : CentralBankProviderBase {
	public BAMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BAM";
	public override string Name => "Bank Al-Maghrib";
	public override ECurrency NativeCurrency => ECurrency.MAD;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrency fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var apiKey = ApiKey;
		if (string.IsNullOrWhiteSpace(apiKey)) {
			throw new InvalidOperationException("Missing CentralBanks:BAM:ApiKey.");
		}

		var url = $"{Url.TrimEnd('/')}/cours/Version1/api/CoursVirement?date={fromDate:yyyy-MM-dd}T12:30:00";
		using var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", apiKey);

		using var response = await Http.SendAsync(request, ct);
		response.EnsureSuccessStatusCode();

		var rawJson = await response.Content.ReadAsStringAsync(ct);
		using var doc = JsonDocument.Parse(rawJson);
		var rates = new List<ExchangeRate>();
		Console.WriteLine(rawJson);
		foreach (var row in doc.RootElement.EnumerateArray()) {
			var code = row.TryGetProperty("libDevise", out var c) ? c.GetString() : null;
			if (string.IsNullOrWhiteSpace(code)) {
				continue;
			}

			var unit = GetDecimal(row, "uniteDevise");
			var mid = GetDecimal(row, "moyen");
			if (mid <= 0) {
				var buy = GetDecimal(row, "achat");
				var sell = GetDecimal(row, "vente");
				if (buy > 0 && sell > 0) {
					mid = (buy + sell) / 2m;
				}
			}

			if (unit <= 0 || mid <= 0) {
				continue;
			}

			rates.Add(new ExchangeRate(from, NativeCurrency, Enum.Parse<ECurrency>(code), mid / unit, Code));
		}
		return rates;
	}
}

/// <summary>
/// Banco de la República
/// </summary>
public sealed class BANREPProvider : CentralBankProviderBase {
	public BANREPProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BANREP";
	public override string Name => "Banco de la República";
	public override ECurrency NativeCurrency => ECurrency.COP;
	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrency fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		var url = $"{Url}?$where=vigenciadesde >= '{fromDate}T00:00:00.000' AND vigenciadesde < '{toDate}T00:00:00.000'";

		using var response = await Http.GetAsync(url, ct);
		response.EnsureSuccessStatusCode();

		using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

		if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0) {
			return [];
		}

		var row = doc.RootElement[0];
		var rate = GetDecimal(row, "valor");

		if (rate <= 0) {
			return [];
		}
		return [new ExchangeRate(fromDate, "USD", NativeCurrency, rate, Code)];
	}
}

/// <summary>
/// Banco de México
/// </summary>
public sealed class BANXICOProvider : CentralBankProviderBase {
	public BANXICOProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BANXICO";
	public override string Name => "Banco de México";
	public override ECurrency NativeCurrency => ECurrency.MXN;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrency fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		if (string.IsNullOrWhiteSpace(ApiKey)) {
			throw new InvalidOperationException("Missing CentralBanks:BANXICO:ApiKey.");
		}

		// Example: USD/MXN FIX exchange rate series.
		const string seriesId = "SF43718";
		var url = $"{Url.TrimEnd('/')}/{seriesId}/datos/{fromDate:yyyy-MM-dd}/{toDate:yyyy-MM-dd}";

		using var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.Add("Bmx-Token", ApiKey);

		using var response = await Http.SendAsync(request, ct);
		response.EnsureSuccessStatusCode();

		using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

		var series = doc.RootElement.GetProperty("bmx")
																		.GetProperty("series")[0];

		if (!series.TryGetProperty("datos", out var data) || data.GetArrayLength() == 0) {
			return [];
		}

		var value = data[0].GetProperty("dato").GetString();

		if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate)) {
			return [];
		}

		return [   new ExchangeRate(fromDate,  "USD",NativeCurrency,rate,Code)
		];
	}
}

/// <summary>
/// Deutsche Bundesbank
/// </summary>
public sealed class BBKProvider : CentralBankProviderBase {
	public BBKProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BBK";
	public override string Name => "Deutsche Bundesbank";
	public override ECurrency NativeCurrency => ECurrency.DEM;
	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrency fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var url = $"{Url}?startPeriod={fromDate:yyyy-MM-dd}&endPeriod={toDate:yyyy-MM-dd}&format=csvdata";

		var csv = await Http.GetStringAsync(url, ct);
		var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

		if (lines.Length < 2) {
			return [];
		}

		var headers = TextUtils.SplitCsv(lines[0]);
		var rates = new List<ExchangeRate>();

		var dateIndex = headers.FindIndex(x => x.Equals("TIME_PERIOD", StringComparison.OrdinalIgnoreCase));
		var valueIndex = headers.FindIndex(x => x.Equals("OBS_VALUE", StringComparison.OrdinalIgnoreCase));
		var currencyIndex = headers.FindIndex(x => x.Equals("CURRENCY", StringComparison.OrdinalIgnoreCase));
		var currencyDenomIndex = headers.FindIndex(x => x.Equals("CURRENCY_DENOM", StringComparison.OrdinalIgnoreCase));

		for (var i = 1; i < lines.Length; i++) {
			var columns = TextUtils.SplitCsv(lines[i]);

			if (dateIndex < 0 || valueIndex < 0 || dateIndex >= columns.Count || valueIndex >= columns.Count) {
				continue;
			}

			if (!DateOnly.TryParse(columns[dateIndex], CultureInfo.InvariantCulture, out var date)) {
				continue;
			}

			if (!decimal.TryParse(columns[valueIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
				continue;
			}

			var baseCurrency = currencyIndex >= 0 && currencyIndex < columns.Count ? columns[currencyIndex] : null;
			var quoteCurrency = currencyDenomIndex >= 0 && currencyDenomIndex < columns.Count ? columns[currencyDenomIndex] : null;

			if (string.IsNullOrWhiteSpace(baseCurrency) || string.IsNullOrWhiteSpace(quoteCurrency)) {
				continue;
			}

			rates.Add(new ExchangeRate(date, baseCurrency, quoteCurrency, rate, Code));
		}

		return rates;
	}

}

/// <summary>
/// Banco Central do Brasil
/// </summary>
public sealed class BCBProvider : CentralBankProviderBase {
	public BCBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCB";
	public override string Name => "Banco Central do Brasil";
	public override ECurrency NativeCurrency => ECurrency.BRL;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrency fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var currencies = new[] { "AUD", "CAD", "CHF", "DKK", "EUR", "GBP", "JPY", "NOK", "SEK", "USD" };
		var rates = new List<ExchangeRate>();

		var url = $"{Url.TrimEnd('/')}/CotacaoMoedaPeriodo(moeda=@moeda,dataInicial=@dataInicial,dataFinalCotacao=@dataFinalCotacao)" +
				$"?@moeda='{fromCurrency}'&@dataInicial='{fromDate:MM-dd-yyyy}'&@dataFinalCotacao='{toDate:MM-dd-yyyy}'&$format=json";


		using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, ct));
		if (!doc.RootElement.TryGetProperty("value", out var values)) {
			return [];
		}

		foreach (var row in values.EnumerateArray()) {
			var buy = GetDecimal(row, "cotacaoCompra");
			var sell = GetDecimal(row, "cotacaoVenda");
			var rate = buy > 0 && sell > 0 ? (buy + sell) / 2m : Math.Max(buy, sell);
			if (rate <= 0) {
				continue;
			}
			var dateText = row.GetProperty("dataHoraCotacao").GetString();

			if (!DateTime.TryParse(dateText, out var dateTime)) {
				continue;
			}
			rates.Add(new ExchangeRate(DateOnly.FromDateTime(dateTime), fromCurrency, NativeCurrency, rate, Code));

		}
		return rates;
	}
}

/// <summary>
/// Banco Central de Bolivia
/// </summary>
public sealed class BCBOProvider : CentralBankProviderBase {
	public BCBOProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCBO";
	public override string Name => "Banco Central de Bolivia";
	public override ECurrency NativeCurrency => ECurrency.BOB;

}

/// <summary>
/// Banco Central de Cuba
/// </summary>
public sealed class BCCProvider : CentralBankProviderBase {
	public BCCProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCC";
	public override string Name => "Banco Central de Cuba";
	public override ECurrency NativeCurrency => ECurrency.CUP;

}

/// <summary>
/// Banco Central de Chile
/// </summary>
public sealed class BCCHProvider : CentralBankProviderBase {
	public BCCHProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCCH";
	public override string Name => "Banco Central de Chile";
	public override ECurrency NativeCurrency => ECurrency.CLP;

}

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

/// <summary>
/// Banque Centrale des Etats de l'Afrique de l'Ouest
/// </summary>
public sealed class BCEAOProvider : CentralBankProviderBase {
	public BCEAOProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCEAO";
	public override string Name => "Banque Centrale des Etats de l'Afrique de l'Ouest";
	public override ECurrency NativeCurrency => ECurrency.XOF;

}

/// <summary>
/// Banco Central de Nicaragua
/// </summary>
public sealed class BCNProvider : CentralBankProviderBase {
	public BCNProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCN";
	public override string Name => "Banco Central de Nicaragua";
	public override ECurrency NativeCurrency => ECurrency.NIO;

}

/// <summary>
/// Banco Central del Paraguay
/// </summary>
public sealed class BCPProvider : CentralBankProviderBase {
	public BCPProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCP";
	public override string Name => "Banco Central del Paraguay";
	public override ECurrency NativeCurrency => ECurrency.PYG;

}

/// <summary>
/// Banco Central de la República Argentina
/// </summary>
public sealed class BCRAProvider : CentralBankProviderBase {
	public BCRAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCRA";
	public override string Name => "Banco Central de la República Argentina";
	public override ECurrency NativeCurrency => ECurrency.ARS;

}

/// <summary>
/// Banque Centrale de Tunisie
/// </summary>
public sealed class BCTProvider : CentralBankProviderBase {
	public BCTProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCT";
	public override string Name => "Banque Centrale de Tunisie";
	public override ECurrency NativeCurrency => ECurrency.TND;

}

/// <summary>
/// Banco Central del Uruguay
/// </summary>
public sealed class BCUProvider : CentralBankProviderBase {
	public BCUProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCU";
	public override string Name => "Banco Central del Uruguay";
	public override ECurrency NativeCurrency => ECurrency.UYU;

}

/// <summary>
/// Banca d'Italia
/// </summary>
public sealed class BDIProvider : CentralBankProviderBase {
	public BDIProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BDI";
	public override string Name => "Banca d'Italia";
	public override ECurrency NativeCurrency => ECurrency.EUR;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrency fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		var url = $"{Url}?referenceDate={date:yyyy-MM-dd}&currencyIsoCode=EUR&lang=en";

		var csv = await Http.GetStringAsync(url, ct);
		var rates = new List<ExchangeRate>();

		foreach (var line in csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1)) {
			var cols = TextUtils.SplitCsv(line);
			if (cols.Count < 3) {
				continue;
			}

			var code = cols.FirstOrDefault(x => x.Length == 3 && x.All(char.IsLetter));
			var rateText = cols.FirstOrDefault(x => decimal.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture, out _));

			if (code is null || rateText is null) {
				continue;
			}

			if (decimal.TryParse(rateText, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) && rate > 0) {
				rates.Add(new ExchangeRate(fromDate, NativeCurrency, code, rate, Code));
			}
		}

		return rates;
	}

}

/// <summary>
/// Banco de Portugal
/// </summary>
public sealed class BDPProvider : CentralBankProviderBase {
	public BDPProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BDP";
	public override string Name => "Banco de Portugal";
	public override ECurrency NativeCurrency => ECurrency.PTE;

}

/// <summary>
/// Bank Indonesia
/// </summary>
public sealed class BIProvider : CentralBankProviderBase {
	public BIProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BI";
	public override string Name => "Bank Indonesia";
	public override ECurrency NativeCurrency => ECurrency.IDR;

}

/// <summary>
/// Banco Nacional de Angola
/// </summary>
public sealed class BNAProvider : CentralBankProviderBase {
	public BNAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BNA";
	public override string Name => "Banco Nacional de Angola";
	public override ECurrency NativeCurrency => ECurrency.AOA;

}

/// <summary>
/// Bank Negara Malaysia
/// </summary>
public sealed class BNMProvider : CentralBankProviderBase {
	public BNMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BNM";
	public override string Name => "Bank Negara Malaysia";
	public override ECurrency NativeCurrency => ECurrency.MYR;

}

/// <summary>
/// Banca Națională a României
/// </summary>
public sealed class BNRProvider : CentralBankProviderBase {
	public BNRProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BNR";
	public override string Name => "Banca Națională a României";
	public override ECurrency NativeCurrency => ECurrency.RON;

}

/// <summary>
/// Banque Nationale du Rwanda
/// </summary>
public sealed class BNRRWProvider : CentralBankProviderBase {
	public BNRRWProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BNRRW";
	public override string Name => "Banque Nationale du Rwanda";
	public override ECurrency NativeCurrency => ECurrency.RWF;

}

/// <summary>
/// Bank of Algeria
/// </summary>
public sealed class BOAProvider : CentralBankProviderBase {
	public BOAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOA";
	public override string Name => "Bank of Algeria";
	public override ECurrency NativeCurrency => ECurrency.DZD;

}

/// <summary>
/// Bank of Botswana
/// </summary>
public sealed class BOBProvider : CentralBankProviderBase {
	public BOBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOB";
	public override string Name => "Bank of Botswana";
	public override ECurrency NativeCurrency => ECurrency.BWP;
}

/// <summary>
/// Bank of Canada
/// </summary>
public sealed class BOCProvider : CentralBankProviderBase {
	public BOCProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOC";
	public override string Name => "Bank of Canada";
	public override ECurrency NativeCurrency => ECurrency.CAD;

	/// <inheritdoc/>
	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrency fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		var url = $"{Url}?start_date={fromDate:yyyy-MM-dd}&end_date={toDate:yyyy-MM-dd}";
		using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, ct));

		if (!doc.RootElement.TryGetProperty("observations", out var observations) || observations.GetArrayLength() == 0) {
			return [];
		}

		var obs = observations[0];
		if (!obs.TryGetProperty("FXUSDCAD", out var series) ||
			!series.TryGetProperty("v", out var value) ||
			!decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var rate)) {
			return [];
		}

		return [new ExchangeRate(date, NativeCurrency, fromCurrency, rate, Code)];
	}
}

/// <summary>
/// Bank of England.
/// Retrieves daily spot exchange rates against Pound Sterling (GBP).
/// </summary>
public sealed class BOEProvider : CentralBankProviderBase {
	public BOEProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }
	private static readonly Dictionary<string, string> Series = new() {
		// Current / recent currencies
		["AUD"] = "XUDLADS",   // Australian Dollar
		["BGN"] = "XUDLZOS3",  // Bulgarian Lev
		["CAD"] = "XUDLCDS",   // Canadian Dollar
		["CNY"] = "XUDLBK89",  // Chinese Yuan
		["CYP"] = "XUDLBK22",  // Cyprus Pound
		["CZK"] = "XUDLBK25",  // Czech Koruna
		["DKK"] = "XUDLDKS",   // Danish Krone
		["EEK"] = "XUDLBK28",  // Estonian Kroon
		["EUR"] = "XUDLERS",   // Euro
		["HKD"] = "XUDLHDS",   // Hong Kong Dollar
		["JPY"] = "XUDLJYS",   // Japanese Yen
		["HUF"] = "XUDLBK33",  // Hungarian Forint
		["INR"] = "XUDLBK97",  // Indian Rupee
		["LVL"] = "XUDLBK39",  // Latvian Lats
		["ILS"] = "XUDLBK78",  // Israeli Shekel
		["LTL"] = "XUDLBK36",  // Lithuanian Litas
		["MYR"] = "XUDLBK83",  // Malaysian Ringgit
		["MTL"] = "XUDLBK44",  // Maltese Lira
		["NZD"] = "XUDLNDS",   // New Zealand Dollar
		["NOK"] = "XUDLNKS",   // Norwegian Krone
		["PLN"] = "XUDLBK47",  // Polish Zloty
		["RON"] = "XUDLZOS4",  // Romanian Leu
		["RUB"] = "XUDLBK68",  // Russian Ruble
		["SAR"] = "XUDLSRS",   // Saudi Riyal
		["SGD"] = "XUDLSGS",   // Singapore Dollar
		["SKK"] = "XUDLBK55",  // Slovak Koruna
		["SEK"] = "XUDLSKS",   // Swedish Krona
		["CHF"] = "XUDLSFS",   // Swiss Franc
		["SIT"] = "XUDLBK52",  // Slovenian Tolar
		["ZAR"] = "XUDLZRS",   // South African Rand
		["KRW"] = "XUDLBK93",  // South Korean Won
		["TWD"] = "XUDLTWS",   // Taiwan Dollar
		["THB"] = "XUDLBK87",  // Thai Baht
		["TRY"] = "XUDLBK95",  // Turkish Lira
		["USD"] = "XUDLUSS",   // US Dollar

		// Legacy pre-Euro currencies
		["ATS"] = "XUDLASS",   // Austrian Schilling
		["BEF"] = "XUDLBFS",   // Belgian Franc
		["DEM"] = "XUDLDMS",   // Deutschemark
		["GRD"] = "XUDLGDS",   // Greek Drachma
		["FIM"] = "XUDLFMS",   // Finnish Markka
		["FRF"] = "XUDLFFS",   // French Franc
		["IEP"] = "XUDLIPS",   // Irish Punt
		["ITL"] = "XUDLILS",   // Italian Lira
		["NLG"] = "XUDLNGS",   // Netherlands Guilder
		["PTE"] = "XUDLPES",   // Portuguese Escudo
		["ESP"] = "XUDLSPS"    // Spanish Peseta
	};
	public override string Code => "BOE";
	public override string Name => "Bank of England";
	public override ECurrency NativeCurrency => ECurrency.GBP;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrency fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		var url = $"{Url}?csv.x=yes" +
			$"&Datefrom={Uri.EscapeDataString($"{fromDate: dd/MMM / yyyy}")}" +
			$"&Dateto={Uri.EscapeDataString($"{toDate: dd /MMM/yyyy)}")}" +
			$"&SeriesCodes={seriesCodes}" +
			"&UsingCodes=Y" +
			"&CSVF=TN";

		var csv = await Http.GetStringAsync(url, ct);
		var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

		if (lines.Length < 2) {
			return [];
		}

		var headers = TextUtils.SplitCsv(lines[0]);
		var values = TextUtils.SplitCsv(lines[1]);

		var rates = new List<ExchangeRate>();

		foreach (var (currency, seriesCode) in Series) {
			var index = headers.FindIndex(x => x.Equals(seriesCode, StringComparison.OrdinalIgnoreCase));
			if (index < 0 || index >= values.Count) {
				continue;
			}

			if (!decimal.TryParse(values[index], NumberStyles.Any, CultureInfo.InvariantCulture, out var rate)) {
				continue;
			}

			rates.Add(new ExchangeRate(date, NativeCurrency, fromCurrency, rate, Code));
		}
		return rates;
	}
}

/// <summary>
/// Bank of Israel
/// </summary>
public sealed class BOIProvider : CentralBankProviderBase {
	public BOIProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOI";
	public override string Name => "Bank of Israel";
	public override ECurrency NativeCurrency => ECurrency.ILS;
	/// 
	/// <summary>
	/// Bank of Israel.
	/// Retrieves representative exchange rates against the Israeli Shekel (ILS).
	/// </summary>

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrency fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var url = $"{Url.TrimEnd('/')}/" +
								$"?c%5BDATA_TYPE%5D=OF00" +
								$"&startperiod={fromDate:yyyy-MM-dd}" +
								$"&endperiod={toDate:yyyy-MM-dd}" +
								$"&format=csv" +
								$"&labels=id";

		var csv = await Http.GetStringAsync(url, ct);
		var rates = new List<ExchangeRate>();

		foreach (var line in csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1)) {
			var columns = TextUtils.SplitCsv(line);

			if (columns.Count < 2) {
				continue;
			}

			var seriesCode = columns.FirstOrDefault(x => x.StartsWith("RER_", StringComparison.OrdinalIgnoreCase));

			if (seriesCode is null) {
				continue;
			}

			// Example:
			// RER_USD_ILS
			var parts = seriesCode.Split('_');

			if (parts.Length < 3) {
				continue;
			}

			var currency = parts[1];
			var rateText = columns.LastOrDefault(x => decimal.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture, out _));

			if (rateText is null) {
				continue;
			}

			if (!decimal.TryParse(rateText, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate)) {
				continue;
			}

			if (rate <= 0) {
				continue;
			}
			rates.Add(new ExchangeRate(date, NativeCurrency, fromCurrency, rate, Code));
		}
		return rates;
	}
}

/// <summary>
/// Bank of Japan
/// </summary>
public sealed class BOJProvider : CentralBankProviderBase {
	public BOJProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOJ";
	public override string Name => "Bank of Japan";
	public override ECurrency NativeCurrency => ECurrency.JPY;
}

/// <summary>
/// Bank of Jamaica
/// </summary>
public sealed class BOJAProvider : CentralBankProviderBase {
	public BOJAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOJA";
	public override string Name => "Bank of Jamaica";
	public override ECurrency NativeCurrency => ECurrency.JMD;
}

/// <summary>
/// Bank of Mongolia
/// </summary>
public sealed class BOMProvider : CentralBankProviderBase {
	public BOMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOM";
	public override string Name => "Bank of Mongolia";
	public override ECurrency NativeCurrency => ECurrency.MNT;
}

/// <summary>
/// Bank of Thailand
/// </summary>
public sealed class BOTProvider : CentralBankProviderBase {
	public BOTProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOT";
	public override string Name => "Bank of Thailand";
	public override ECurrency NativeCurrency => ECurrency.THB;
}

/// <summary>
/// Bank of Tanzania
/// </summary>
public sealed class BOTAProvider : CentralBankProviderBase {
	public BOTAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOTA";
	public override string Name => "Bank of Tanzania";
	public override ECurrency NativeCurrency => ECurrency.TZS;
}

/// <summary>
/// Banque de la Republique du Burundi
/// </summary>
public sealed class BRBProvider : CentralBankProviderBase {
	public BRBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BRB";
	public override string Name => "Banque de la Republique du Burundi";
	public override ECurrency NativeCurrency => ECurrency.BIF;
}

/// <summary>
/// Bangko Sentral ng Pilipinas
/// </summary>
public sealed class BSPProvider : CentralBankProviderBase {
	public BSPProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BSP";
	public override string Name => "Bangko Sentral ng Pilipinas";
	public override ECurrency NativeCurrency => ECurrency.PHP;
}

/// <summary>
/// Central Bank of Armenia
/// </summary>
public sealed class CBAProvider : CentralBankProviderBase {
	public CBAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBA";
	public override string Name => "Central Bank of Armenia";
	public override ECurrency NativeCurrency => ECurrency.AMD;
}

/// <summary>
/// Central Bank of the Republic of China (Taiwan)
/// </summary>
public sealed class CBCProvider : CentralBankProviderBase {
	public CBCProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBC";
	public override string Name => "Central Bank of the Republic of China (Taiwan)";
	public override ECurrency NativeCurrency => ECurrency.TWD;
}

/// <summary>
/// Central Bank of Egypt
/// </summary>
public sealed class CBEProvider : CentralBankProviderBase {
	public CBEProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBE";
	public override string Name => "Central Bank of Egypt";
	public override ECurrency NativeCurrency => ECurrency.EGP;
}

/// <summary>
/// Central Bank of The Gambia
/// </summary>
public sealed class CBGProvider : CentralBankProviderBase {
	public CBGProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBG";
	public override string Name => "Central Bank of The Gambia";
	public override ECurrency NativeCurrency => ECurrency.GMD;
}

/// <summary>
/// Central Bank of Iraq
/// </summary>
public sealed class CBIProvider : CentralBankProviderBase {
	public CBIProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBI";
	public override string Name => "Central Bank of Iraq";
	public override ECurrency NativeCurrency => ECurrency.IQD;
}

/// <summary>
/// Central Bank of Kenya
/// </summary>
public sealed class CBKProvider : CentralBankProviderBase {
	public CBKProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBK";
	public override string Name => "Central Bank of Kenya";
	public override ECurrency NativeCurrency => ECurrency.KES;
}

/// <summary>
/// Central Bank of Liberia
/// </summary>
public sealed class CBLLRProvider : CentralBankProviderBase {
	public CBLLRProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBLLR";
	public override string Name => "Central Bank of Liberia";
	public override ECurrency NativeCurrency => ECurrency.LRD;
}

/// <summary>
/// Central Bank of Myanmar
/// </summary>
public sealed class CBMProvider : CentralBankProviderBase {
	public CBMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBM";
	public override string Name => "Central Bank of Myanmar";
	public override ECurrency NativeCurrency => ECurrency.MMK;
}

/// <summary>
/// Central Bank of Nigeria
/// </summary>
public sealed class CBNProvider : CentralBankProviderBase {
	public CBNProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBN";
	public override string Name => "Central Bank of Nigeria";
	public override ECurrency NativeCurrency => ECurrency.NGN;
}

/// <summary>
/// Central Bank of Russia
/// </summary>
public sealed class CBRProvider : CentralBankProviderBase {
	public CBRProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBR";
	public override string Name => "Central Bank of Russia";
	public override ECurrency NativeCurrency => ECurrency.RUB;
}

/// <summary>
/// Central Bank of Samoa
/// </summary>
public sealed class CBSProvider : CentralBankProviderBase {
	public CBSProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBS";
	public override string Name => "Central Bank of Samoa";
	public override ECurrency NativeCurrency => ECurrency.WST;
}

/// <summary>
/// Central Bank of Sri Lanka
/// </summary>
public sealed class CBSLProvider : CentralBankProviderBase {
	public CBSLProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBSL";
	public override string Name => "Central Bank of Sri Lanka";
	public override ECurrency NativeCurrency => ECurrency.LKR;
}

/// <summary>
/// Central Bank of Uzbekistan
/// </summary>
public sealed class CBUProvider : CentralBankProviderBase {
	public CBUProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBU";
	public override string Name => "Central Bank of Uzbekistan";
	public override ECurrency NativeCurrency => ECurrency.UZS;
}

/// <summary>
/// Czech National Bank
/// </summary>
public sealed class CNBProvider : CentralBankProviderBase {
	public CNBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CNB";
	public override string Name => "Czech National Bank";
	public override ECurrency NativeCurrency => ECurrency.CZK;
}

/// <summary>
/// Da Afghanistan Bank
/// </summary>
public sealed class DABProvider : CentralBankProviderBase {
	public DABProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "DAB";
	public override string Name => "Da Afghanistan Bank";
	public override ECurrency NativeCurrency => ECurrency.AFN;
}

/// <summary>
/// Danmarks Nationalbank
/// </summary>
public sealed class DNBProvider : CentralBankProviderBase {
	public DNBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "DNB";
	public override string Name => "Danmarks Nationalbank";
	public override ECurrency NativeCurrency => ECurrency.DKK;
}

/// <summary>
/// European Central Bank
/// </summary>
public sealed class ECBProvider : CentralBankProviderBase {
	public ECBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "ECB";
	public override string Name => "European Central Bank";
	public override ECurrency NativeCurrency => ECurrency.EUR;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrency fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var url = $"{Url}?startPeriod={fromDate:yyyy-MM-dd}&endPeriod={toDate:yyyy-MM-dd}&format=csvdata";

		var csv = await Http.GetStringAsync(url, ct);
		var rates = new List<ExchangeRate>();

		foreach (var line in csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1)) {
			var cols = TextUtils.SplitCsv(line);
			if (cols.Count < 2) {
				continue;
			}

			var currency = cols.FirstOrDefault(x => x.Length == 3 && x.All(char.IsLetter) && x != "EUR");
			var numeric = cols.LastOrDefault(x => decimal.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture, out _));

			if (currency is null || numeric is null) {
				continue;
			}

			if (decimal.TryParse(numeric, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) && rate > 0) {
				rates.Add(new ExchangeRate(date, NativeCurrency, fromCurrency, rate, Code));
			}
		}
		return rates;
	}
}

/// <summary>
/// Financial Benchmarks India
/// </summary>
public sealed class FBILProvider : CentralBankProviderBase {
	public FBILProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "FBIL";
	public override string Name => "Financial Benchmarks India";
	public override ECurrency NativeCurrency => ECurrency.INR;
}

/// <summary>
/// Federal Reserve Bank of St. Louis
/// </summary>
public sealed class FREDProvider : CentralBankProviderBase {
	public FREDProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "FRED";
	public override string Name => "Federal Reserve Bank of St. Louis";
	public override ECurrency NativeCurrency => ECurrency.USD;
}

/// <summary>
/// Hong Kong Monetary Authority
/// </summary>
public sealed class HKMAProvider : CentralBankProviderBase {
	public HKMAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "HKMA";
	public override string Name => "Hong Kong Monetary Authority";
	public override ECurrency NativeCurrency => ECurrency.HKD;
}

/// <summary>
/// Hrvatska Narodna Banka
/// </summary>
public sealed class HNBProvider : CentralBankProviderBase {
	public HNBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "HNB";
	public override string Name => "Hrvatska Narodna Banka";
	public override ECurrency NativeCurrency => ECurrency.EUR;
}

/// <summary>
/// International Monetary Fund
/// </summary>
public sealed class IMFProvider : CentralBankProviderBase {
	public IMFProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "IMF";
	public override string Name => "International Monetary Fund";
	public override ECurrency NativeCurrency => ECurrency.XDR;
}

/// <summary>
/// Lietuvos Bankas
/// </summary>
public sealed class LBProvider : CentralBankProviderBase {
	public LBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "LB";
	public override string Name => "Lietuvos Bankas";
	public override ECurrency NativeCurrency => ECurrency.EUR;
}

/// <summary>
/// Monetary Authority of Singapore
/// </summary>
public sealed class MASProvider : CentralBankProviderBase {
	public MASProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "MAS";
	public override string Name => "Monetary Authority of Singapore";
	public override ECurrency NativeCurrency => ECurrency.SGD;
}

/// <summary>
/// Maldives Monetary Authority
/// </summary>
public sealed class MMAProvider : CentralBankProviderBase {
	public MMAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "MMA";
	public override string Name => "Maldives Monetary Authority";
	public override ECurrency NativeCurrency => ECurrency.MVR;
}

/// <summary>
/// Magyar Nemzeti Bank
/// </summary>
public sealed class MNBProvider : CentralBankProviderBase {
	public MNBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "MNB";
	public override string Name => "Magyar Nemzeti Bank";
	public override ECurrency NativeCurrency => ECurrency.HUF;
}

/// <summary>
/// Norges Bank
/// </summary>
public sealed class NBProvider : CentralBankProviderBase {
	public NBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NB";
	public override string Name => "Norges Bank";
	public override ECurrency NativeCurrency => ECurrency.NOK;
}

/// <summary>
/// National Bank of Cambodia
/// </summary>
public sealed class NBCProvider : CentralBankProviderBase {
	public NBCProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBC";
	public override string Name => "National Bank of Cambodia";
	public override ECurrency NativeCurrency => ECurrency.KHR;
}

/// <summary>
/// National Bank of Ethiopia
/// </summary>
public sealed class NBEProvider : CentralBankProviderBase {
	public NBEProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBE";
	public override string Name => "National Bank of Ethiopia";
	public override ECurrency NativeCurrency => ECurrency.ETB;
}

/// <summary>
/// National Bank of Georgia
/// </summary>
public sealed class NBGProvider : CentralBankProviderBase {
	public NBGProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBG";
	public override string Name => "National Bank of Georgia";
	public override ECurrency NativeCurrency => ECurrency.GEL;
}

/// <summary>
/// National Bank of Kazakhstan
/// </summary>
public sealed class NBKProvider : CentralBankProviderBase {
	public NBKProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBK";
	public override string Name => "National Bank of Kazakhstan";
	public override ECurrency NativeCurrency => ECurrency.KZT;
}

/// <summary>
/// National Bank of the Kyrgyz Republic
/// </summary>
public sealed class NBKRProvider : CentralBankProviderBase {
	public NBKRProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBKR";
	public override string Name => "National Bank of the Kyrgyz Republic";
	public override ECurrency NativeCurrency => ECurrency.KGS;

}

/// <summary>
/// National Bank of Moldova
/// </summary>
public sealed class NBMProvider : CentralBankProviderBase {
	public NBMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBM";
	public override string Name => "National Bank of Moldova";
	public override ECurrency NativeCurrency => ECurrency.MDL;
}

/// <summary>
/// Narodowy Bank Polski
/// </summary>
public sealed class NBPProvider : CentralBankProviderBase {
	public NBPProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBP";
	public override string Name => "Narodowy Bank Polski";
	public override ECurrency NativeCurrency => ECurrency.PLN;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrency fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var url = $"{Url.TrimEnd('/')}/tables/A/{fromDate:yyyy-MM-dd}?format=json";
		using var response = await Http.GetAsync(url, ct);

		if (response.StatusCode == System.Net.HttpStatusCode.NotFound) {
			return [];
		}

		response.EnsureSuccessStatusCode();
		using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

		var rates = new List<ExchangeRate>();
		var table = doc.RootElement[0];

		foreach (var row in table.GetProperty("rates").EnumerateArray()) {
			var code = row.GetProperty("code").GetString();
			var rate = GetDecimal(row, "mid");

			if (!string.IsNullOrWhiteSpace(code) && rate > 0) {
				rates.Add(new ExchangeRate(fromDate, NativeCurrency, code!, rate, Code));
			}
		}
		return rates;
	}
}

/// <summary>
/// Natsyyanalny Bank Respubliki Belarus
/// </summary>
public sealed class NBRBProvider : CentralBankProviderBase {
	public NBRBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBRB";
	public override string Name => "Natsyyanalny Bank Respubliki Belarus";
	public override ECurrency NativeCurrency => ECurrency.BYN;
}

/// <summary>
/// Narodna Banka na Republika Severna Makedonija
/// </summary>
public sealed class NBRMProvider : CentralBankProviderBase {
	public NBRMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBRM";
	public override string Name => "Narodna Banka na Republika Severna Makedonija";
	public override ECurrency NativeCurrency => ECurrency.MKD;
}

/// <summary>
/// National Bank of Tajikistan
/// </summary>
public sealed class NBTProvider : CentralBankProviderBase {
	public NBTProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBT";
	public override string Name => "National Bank of Tajikistan";
	public override ECurrency NativeCurrency => ECurrency.TJS;
}

/// <summary>
/// Natsionalnyi Bank Ukrainy
/// </summary>
public sealed class NBUProvider : CentralBankProviderBase {
	public NBUProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBU";
	public override string Name => "Natsionalnyi Bank Ukrainy";
	public override ECurrency NativeCurrency => ECurrency.UAH;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrency fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var url =
			$"{Url}?date={fromDate:yyyyMMdd}&json";

		using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, ct));
		var rates = new List<ExchangeRate>();

		foreach (var row in doc.RootElement.EnumerateArray()) {
			var code = row.TryGetProperty("cc", out var c) ? c.GetString() : null;
			var rate = GetDecimal(row, "rate");

			if (!string.IsNullOrWhiteSpace(code) && rate > 0) {
				rates.Add(new ExchangeRate(fromDate, code!, NativeCurrency, rate, Code));
			}
		}
		return rates;
	}
}

/// <summary>
/// Nepal Rastra Bank
/// </summary>
public sealed class NRBProvider : CentralBankProviderBase {
	public NRBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NRB";
	public override string Name => "Nepal Rastra Bank";
	public override ECurrency NativeCurrency => ECurrency.NPR;
}

/// <summary>
/// National Reserve Bank of Tonga
/// </summary>
public sealed class NRBTProvider : CentralBankProviderBase {
	public NRBTProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NRBT";
	public override string Name => "National Reserve Bank of Tonga";
	public override ECurrency NativeCurrency => ECurrency.TOP;
}

/// <summary>
/// Sveriges Riksbank
/// </summary>
public sealed class RBProvider : CentralBankProviderBase {
	public RBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "RB";
	public override string Name => "Sveriges Riksbank";
	public override ECurrency NativeCurrency => ECurrency.SEK;
}

/// <summary>
/// Reserve Bank of Australia
/// </summary>
public sealed class RBAProvider : CentralBankProviderBase {
	public RBAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "RBA";
	public override string Name => "Reserve Bank of Australia";
	public override ECurrency NativeCurrency => ECurrency.AUD;

}

/// <summary>
/// Reserve Bank of Fiji
/// </summary>
public sealed class RBFProvider : CentralBankProviderBase {
	public RBFProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "RBF";
	public override string Name => "Reserve Bank of Fiji";
	public override ECurrency NativeCurrency => ECurrency.FJD;
}

/// <summary>
/// Reserve Bank of Malawi
/// </summary>
public sealed class RBMProvider : CentralBankProviderBase {
	public RBMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "RBM";
	public override string Name => "Reserve Bank of Malawi";
	public override ECurrency NativeCurrency => ECurrency.MWK;
}

/// <summary>
/// Reserve Bank of Vanuatu
/// </summary>
public sealed class RBVProvider : CentralBankProviderBase {
	public RBVProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "RBV";
	public override string Name => "Reserve Bank of Vanuatu";
	public override ECurrency NativeCurrency => ECurrency.VUV;
}

/// <summary>
/// South African Reserve Bank
/// </summary>
public sealed class SARBProvider : CentralBankProviderBase {
	public SARBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "SARB";
	public override string Name => "South African Reserve Bank";
	public override ECurrency NativeCurrency => ECurrency.ZAR;
}

/// <summary>
/// Seðlabanki Íslands
/// </summary>
public sealed class SBIProvider : CentralBankProviderBase {
	public SBIProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "SBI";
	public override string Name => "Seðlabanki Íslands";
	public override ECurrency NativeCurrency => ECurrency.ISK;
}

/// <summary>
/// State Bank of Pakistan
/// </summary>
public sealed class SBPProvider : CentralBankProviderBase {
	public SBPProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "SBP";
	public override string Name => "State Bank of Pakistan";
	public override ECurrency NativeCurrency => ECurrency.PKR;
}

/// <summary>
/// Türkiye Cumhuriyet Merkez Bankası
/// </summary>
public sealed class TCMBProvider : CentralBankProviderBase {
	public TCMBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "TCMB";
	public override string Name => "Türkiye Cumhuriyet Merkez Bankası";
	public override ECurrency NativeCurrency => ECurrency.TRY;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrency fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var url = $"{Url.TrimEnd('/')}/{fromDate:yyyyMM}/{toDate:ddMMyyyy}.xml";
		var xml = await Http.GetStringAsync(url, ct);

		var doc = System.Xml.Linq.XDocument.Parse(xml);
		var rates = new List<ExchangeRate>();

		foreach (var node in doc.Descendants("Currency")) {
			var code = node.Attribute("CurrencyCode")?.Value;
			var unitText = node.Element("Unit")?.Value;
			var forexBuyingText = node.Element("ForexBuying")?.Value;
			var forexSellingText = node.Element("ForexSelling")?.Value;

			if (string.IsNullOrWhiteSpace(code) ||
				!decimal.TryParse(unitText, NumberStyles.Any, CultureInfo.InvariantCulture, out var unit) ||
				unit <= 0) {
				continue;
			}

			decimal.TryParse(forexBuyingText, NumberStyles.Any, CultureInfo.InvariantCulture, out var buy);
			decimal.TryParse(forexSellingText, NumberStyles.Any, CultureInfo.InvariantCulture, out var sell);

			var mid = buy > 0 && sell > 0 ? (buy + sell) / 2m : Math.Max(buy, sell);
			if (mid > 0) {
				rates.Add(new ExchangeRate(fromDate, code, NativeCurrency, mid / unit, Code));
			}
		}
		return rates;
	}
}

public sealed class CentralBankProviderFactory {
	private readonly IServiceProvider _services;

	public CentralBankProviderFactory(IServiceProvider services) {
		_services = services;
	}
	public IEnumerable<ICentralBankProvider> GetAll() {
		return _services.GetServices<ICentralBankProvider>();
	}

	public ICentralBankProvider Get(string providerCode)
		=> providerCode.ToUpperInvariant() switch {
			"AMCM" => _services.GetRequiredService<AMCMProvider>(),
			"BAM" => _services.GetRequiredService<BAMProvider>(),
			"BANREP" => _services.GetRequiredService<BANREPProvider>(),
			"BANXICO" => _services.GetRequiredService<BANXICOProvider>(),
			"BBK" => _services.GetRequiredService<BBKProvider>(),
			"BCB" => _services.GetRequiredService<BCBProvider>(),
			"BCBO" => _services.GetRequiredService<BCBOProvider>(),
			"BCC" => _services.GetRequiredService<BCCProvider>(),
			"BCCH" => _services.GetRequiredService<BCCHProvider>(),
			"BCCR" => _services.GetRequiredService<BCCRProvider>(),
			"BCEAO" => _services.GetRequiredService<BCEAOProvider>(),
			"BCN" => _services.GetRequiredService<BCNProvider>(),
			"BCP" => _services.GetRequiredService<BCPProvider>(),
			"BCRA" => _services.GetRequiredService<BCRAProvider>(),
			"BCT" => _services.GetRequiredService<BCTProvider>(),
			"BCU" => _services.GetRequiredService<BCUProvider>(),
			"BDI" => _services.GetRequiredService<BDIProvider>(),
			"BDP" => _services.GetRequiredService<BDPProvider>(),
			"BI" => _services.GetRequiredService<BIProvider>(),
			"BNA" => _services.GetRequiredService<BNAProvider>(),
			"BNM" => _services.GetRequiredService<BNMProvider>(),
			"BNR" => _services.GetRequiredService<BNRProvider>(),
			"BNRRW" => _services.GetRequiredService<BNRRWProvider>(),
			"BOA" => _services.GetRequiredService<BOAProvider>(),
			"BOB" => _services.GetRequiredService<BOBProvider>(),
			"BOC" => _services.GetRequiredService<BOCProvider>(),
			"BOE" => _services.GetRequiredService<BOEProvider>(),
			"BOI" => _services.GetRequiredService<BOIProvider>(),
			"BOJ" => _services.GetRequiredService<BOJProvider>(),
			"BOJA" => _services.GetRequiredService<BOJAProvider>(),
			"BOM" => _services.GetRequiredService<BOMProvider>(),
			"BOT" => _services.GetRequiredService<BOTProvider>(),
			"BOTA" => _services.GetRequiredService<BOTAProvider>(),
			"BRB" => _services.GetRequiredService<BRBProvider>(),
			"BSP" => _services.GetRequiredService<BSPProvider>(),
			"CBA" => _services.GetRequiredService<CBAProvider>(),
			"CBC" => _services.GetRequiredService<CBCProvider>(),
			"CBE" => _services.GetRequiredService<CBEProvider>(),
			"CBG" => _services.GetRequiredService<CBGProvider>(),
			"CBI" => _services.GetRequiredService<CBIProvider>(),
			"CBK" => _services.GetRequiredService<CBKProvider>(),
			"CBLLR" => _services.GetRequiredService<CBLLRProvider>(),
			"CBM" => _services.GetRequiredService<CBMProvider>(),
			"CBN" => _services.GetRequiredService<CBNProvider>(),
			"CBR" => _services.GetRequiredService<CBRProvider>(),
			"CBS" => _services.GetRequiredService<CBSProvider>(),
			"CBSL" => _services.GetRequiredService<CBSLProvider>(),
			"CBU" => _services.GetRequiredService<CBUProvider>(),
			"CNB" => _services.GetRequiredService<CNBProvider>(),
			"DAB" => _services.GetRequiredService<DABProvider>(),
			"DNB" => _services.GetRequiredService<DNBProvider>(),
			"ECB" => _services.GetRequiredService<ECBProvider>(),
			"FBIL" => _services.GetRequiredService<FBILProvider>(),
			"FRED" => _services.GetRequiredService<FREDProvider>(),
			"HKMA" => _services.GetRequiredService<HKMAProvider>(),
			"HNB" => _services.GetRequiredService<HNBProvider>(),
			"IMF" => _services.GetRequiredService<IMFProvider>(),
			"LB" => _services.GetRequiredService<LBProvider>(),
			"MAS" => _services.GetRequiredService<MASProvider>(),
			"MMA" => _services.GetRequiredService<MMAProvider>(),
			"MNB" => _services.GetRequiredService<MNBProvider>(),
			"NB" => _services.GetRequiredService<NBProvider>(),
			"NBC" => _services.GetRequiredService<NBCProvider>(),
			"NBE" => _services.GetRequiredService<NBEProvider>(),
			"NBG" => _services.GetRequiredService<NBGProvider>(),
			"NBK" => _services.GetRequiredService<NBKProvider>(),
			"NBKR" => _services.GetRequiredService<NBKRProvider>(),
			"NBM" => _services.GetRequiredService<NBMProvider>(),
			"NBP" => _services.GetRequiredService<NBPProvider>(),
			"NBRB" => _services.GetRequiredService<NBRBProvider>(),
			"NBRM" => _services.GetRequiredService<NBRMProvider>(),
			"NBT" => _services.GetRequiredService<NBTProvider>(),
			"NBU" => _services.GetRequiredService<NBUProvider>(),
			"NRB" => _services.GetRequiredService<NRBProvider>(),
			"NRBT" => _services.GetRequiredService<NRBTProvider>(),
			"RB" => _services.GetRequiredService<RBProvider>(),
			"RBA" => _services.GetRequiredService<RBAProvider>(),
			"RBF" => _services.GetRequiredService<RBFProvider>(),
			"RBM" => _services.GetRequiredService<RBMProvider>(),
			"RBV" => _services.GetRequiredService<RBVProvider>(),
			"SARB" => _services.GetRequiredService<SARBProvider>(),
			"SBI" => _services.GetRequiredService<SBIProvider>(),
			"SBP" => _services.GetRequiredService<SBPProvider>(),
			"TCMB" => _services.GetRequiredService<TCMBProvider>(),
			_ => throw new ArgumentOutOfRangeException(nameof(providerCode), providerCode, "Unknown central-bank provider.")
		};
}