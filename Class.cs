namespace ExchangeRates.Server;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using System.Globalization;
using System.Text.Json;

public sealed record ExchangeRate(
	DateOnly Date,
	string BaseCurrency,
	string QuoteCurrency,
	decimal Rate,
	string Provider);

public interface ICentralBankProvider {
	string Code { get; }
	string Name { get; }
	string NativeCurrency { get; }

	Task<IReadOnlyList<ExchangeRate>> GetRatesAsync(
		DateOnly? date = null,
		CancellationToken cancellationToken = default);
}

public abstract class CentralBankProviderBase : ICentralBankProvider {
	protected CentralBankProviderBase(HttpClient http, IConfiguration configuration) {
		Http = http;
		Configuration = configuration;
	}

	protected HttpClient Http { get; }
	protected IConfiguration Configuration { get; }

	public abstract string Code { get; }
	public abstract string Name { get; }
	public abstract string NativeCurrency { get; }

	public Task<IReadOnlyList<ExchangeRate>> GetRatesAsync(
		DateOnly? date = null,
		CancellationToken cancellationToken = default)
		=> FetchAsync(date ?? DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);

	protected virtual Task<IReadOnlyList<ExchangeRate>> FetchAsync(
		DateOnly date,
		CancellationToken cancellationToken)
		=> throw new NotSupportedException(
			$"{Code} ({Name}) is registered, but its direct-source adapter still needs a bank-specific parser/endpoint implementation.");

	protected static decimal GetDecimal(JsonElement element, string propertyName) {
		if (!element.TryGetProperty(propertyName, out var property)) {
			return 0m;
		}

		if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var number)) {
			return number;
		}

		if (property.ValueKind == JsonValueKind.String &&
			decimal.TryParse(property.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)) {
			return parsed;
		}

		return 0m;
	}

	protected static List<string> SplitCsv(string line) {
		var result = new List<string>();
		var current = new System.Text.StringBuilder();
		var quoted = false;

		for (var i = 0; i < line.Length; i++) {
			var ch = line[i];

			if (ch == '"') {
				if (quoted && i + 1 < line.Length && line[i + 1] == '"') {
					current.Append('"');
					i++;
				}
				else {
					quoted = !quoted;
				}
			}
			else if (ch == ',' && !quoted) {
				result.Add(current.ToString().Trim());
				current.Clear();
			}
			else {
				current.Append(ch);
			}
		}

		result.Add(current.ToString().Trim());
		return result;
	}
}

public sealed class AMCMProvider : CentralBankProviderBase {
	public AMCMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "AMCM";
	public override string Name => "Monetary Authority of Macao";
	public override string NativeCurrency => "MOP";

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(DateOnly date, CancellationToken cancellationToken) {
		var from = date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
		var url = $"https://www.amcm.gov.mo/api/v1.0/cms/financial_info?QueryType=1&Begin={from}&End={from}";
		using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, cancellationToken));
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

			rates.Add(new ExchangeRate(date, code!, "MOP", value / unit, Code));
		}

		return rates;
	}

}

public sealed class BAMProvider : CentralBankProviderBase {
	public BAMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BAM";
	public override string Name => "Bank Al-Maghrib";
	public override string NativeCurrency => "MAD";

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(DateOnly date, CancellationToken cancellationToken) {
		var apiKey = Configuration["CentralBanks:BAM:ApiKey"];
		if (string.IsNullOrWhiteSpace(apiKey)) {
			throw new InvalidOperationException("Missing CentralBanks:BAM:ApiKey.");
		}

		var url = $"https://api.centralbankofmorocco.ma/cours/Version1/api/CoursVirement?date={date:yyyy-MM-dd}T12:30:00";
		using var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", apiKey);

		using var response = await Http.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();

		using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
		var rates = new List<ExchangeRate>();

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

			rates.Add(new ExchangeRate(date, code!, "MAD", mid / unit, Code));
		}

		return rates;
	}

}

public sealed class BANREPProvider : CentralBankProviderBase {
	public BANREPProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BANREP";
	public override string Name => "Banco de la República";
	public override string NativeCurrency => "COP";

}

public sealed class BANXICOProvider : CentralBankProviderBase {
	public BANXICOProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BANXICO";
	public override string Name => "Banco de México";
	public override string NativeCurrency => "MXN";

}

public sealed class BBKProvider : CentralBankProviderBase {
	public BBKProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BBK";
	public override string Name => "Deutsche Bundesbank";
	public override string NativeCurrency => "DEM";

}

public sealed class BCBProvider : CentralBankProviderBase {
	public BCBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCB";
	public override string Name => "Banco Central do Brasil";
	public override string NativeCurrency => "BRL";

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(DateOnly date, CancellationToken cancellationToken) {
		var currencies = new[] { "AUD", "CAD", "CHF", "DKK", "EUR", "GBP", "JPY", "NOK", "SEK", "USD" };
		var rates = new List<ExchangeRate>();

		foreach (var currency in currencies) {
			var url =
				"https://olinda.bcb.gov.br/olinda/servico/PTAX/versao/v1/odata/" +
				"CotacaoMoedaDia(moeda=@moeda,dataCotacao=@dataCotacao)" +
				$"?@moeda='{currency}'&@dataCotacao='{date:MM-dd-yyyy}'&$format=json";

			using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, cancellationToken));
			if (!doc.RootElement.TryGetProperty("value", out var value) || value.GetArrayLength() == 0) {
				continue;
			}

			var rows = value.EnumerateArray().ToArray();
			var row = rows[^1];
			var buy = GetDecimal(row, "cotacaoCompra");
			var sell = GetDecimal(row, "cotacaoVenda");
			var mid = buy > 0 && sell > 0 ? (buy + sell) / 2m : Math.Max(buy, sell);

			if (mid > 0) {
				rates.Add(new ExchangeRate(date, currency, "BRL", mid, Code));
			}
		}

		return rates;
	}

}

public sealed class BCBOProvider : CentralBankProviderBase {
	public BCBOProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCBO";
	public override string Name => "Banco Central de Bolivia";
	public override string NativeCurrency => "BOB";

}

public sealed class BCCProvider : CentralBankProviderBase {
	public BCCProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCC";
	public override string Name => "Banco Central de Cuba";
	public override string NativeCurrency => "CUP";

}

public sealed class BCCHProvider : CentralBankProviderBase {
	public BCCHProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCCH";
	public override string Name => "Banco Central de Chile";
	public override string NativeCurrency => "CLP";

}

public sealed class BCCRProvider : CentralBankProviderBase {
	public BCCRProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCCR";
	public override string Name => "Banco Central de Costa Rica";
	public override string NativeCurrency => "CRC";

}

public sealed class BCEAOProvider : CentralBankProviderBase {
	public BCEAOProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCEAO";
	public override string Name => "Banque Centrale des Etats de l'Afrique de l'Ouest";
	public override string NativeCurrency => "XOF";

}

public sealed class BCNProvider : CentralBankProviderBase {
	public BCNProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCN";
	public override string Name => "Banco Central de Nicaragua";
	public override string NativeCurrency => "NIO";

}

public sealed class BCPProvider : CentralBankProviderBase {
	public BCPProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCP";
	public override string Name => "Banco Central del Paraguay";
	public override string NativeCurrency => "PYG";

}

public sealed class BCRAProvider : CentralBankProviderBase {
	public BCRAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCRA";
	public override string Name => "Banco Central de la República Argentina";
	public override string NativeCurrency => "ARS";

}

public sealed class BCTProvider : CentralBankProviderBase {
	public BCTProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCT";
	public override string Name => "Banque Centrale de Tunisie";
	public override string NativeCurrency => "TND";

}

public sealed class BCUProvider : CentralBankProviderBase {
	public BCUProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCU";
	public override string Name => "Banco Central del Uruguay";
	public override string NativeCurrency => "UYU";

}

public sealed class BDIProvider : CentralBankProviderBase {
	public BDIProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BDI";
	public override string Name => "Banca d'Italia";
	public override string NativeCurrency => "EUR";

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(DateOnly date, CancellationToken cancellationToken) {
		var url =
			"https://tassidicambio.bancaditalia.it/terzevalute-wf-web/rest/v1.0/dailyRates" +
			$"?referenceDate={date:yyyy-MM-dd}&currencyIsoCode=EUR&lang=en";

		var csv = await Http.GetStringAsync(url, cancellationToken);
		var rates = new List<ExchangeRate>();

		foreach (var line in csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1)) {
			var cols = SplitCsv(line);
			if (cols.Count < 3) {
				continue;
			}

			var code = cols.FirstOrDefault(x => x.Length == 3 && x.All(char.IsLetter));
			var rateText = cols.FirstOrDefault(x => decimal.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture, out _));

			if (code is null || rateText is null) {
				continue;
			}

			if (decimal.TryParse(rateText, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) && rate > 0) {
				rates.Add(new ExchangeRate(date, "EUR", code.ToUpperInvariant(), rate, Code));
			}
		}

		return rates;
	}

}

public sealed class BDPProvider : CentralBankProviderBase {
	public BDPProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BDP";
	public override string Name => "Banco de Portugal";
	public override string NativeCurrency => "PTE";

}

public sealed class BIProvider : CentralBankProviderBase {
	public BIProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BI";
	public override string Name => "Bank Indonesia";
	public override string NativeCurrency => "IDR";

}

public sealed class BNAProvider : CentralBankProviderBase {
	public BNAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BNA";
	public override string Name => "Banco Nacional de Angola";
	public override string NativeCurrency => "AOA";

}

public sealed class BNMProvider : CentralBankProviderBase {
	public BNMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BNM";
	public override string Name => "Bank Negara Malaysia";
	public override string NativeCurrency => "MYR";

}

public sealed class BNRProvider : CentralBankProviderBase {
	public BNRProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BNR";
	public override string Name => "Banca Națională a României";
	public override string NativeCurrency => "RON";

}

public sealed class BNRRWProvider : CentralBankProviderBase {
	public BNRRWProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BNRRW";
	public override string Name => "Banque Nationale du Rwanda";
	public override string NativeCurrency => "RWF";

}

public sealed class BOAProvider : CentralBankProviderBase {
	public BOAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOA";
	public override string Name => "Bank of Algeria";
	public override string NativeCurrency => "DZD";

}

public sealed class BOBProvider : CentralBankProviderBase {
	public BOBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOB";
	public override string Name => "Bank of Botswana";
	public override string NativeCurrency => "BWP";

}

public sealed class BOCProvider : CentralBankProviderBase {
	public BOCProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOC";
	public override string Name => "Bank of Canada";
	public override string NativeCurrency => "CAD";

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(DateOnly date, CancellationToken cancellationToken) {
		var start = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		var url = $"https://www.bankofcanada.ca/valet/observations/FXUSDCAD/json?start_date={start}&end_date={start}";
		using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, cancellationToken));

		if (!doc.RootElement.TryGetProperty("observations", out var observations) || observations.GetArrayLength() == 0) {
			return [];
		}

		var obs = observations[0];
		if (!obs.TryGetProperty("FXUSDCAD", out var series) ||
			!series.TryGetProperty("v", out var value) ||
			!decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var rate)) {
			return [];
		}

		return [new ExchangeRate(date, "USD", "CAD", rate, Code)];
	}

}

public sealed class BOEProvider : CentralBankProviderBase {
	public BOEProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOE";
	public override string Name => "Bank of England";
	public override string NativeCurrency => "GBP";

}

public sealed class BOIProvider : CentralBankProviderBase {
	public BOIProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOI";
	public override string Name => "Bank of Israel";
	public override string NativeCurrency => "ILS";

}

public sealed class BOJProvider : CentralBankProviderBase {
	public BOJProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOJ";
	public override string Name => "Bank of Japan";
	public override string NativeCurrency => "JPY";

}

public sealed class BOJAProvider : CentralBankProviderBase {
	public BOJAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOJA";
	public override string Name => "Bank of Jamaica";
	public override string NativeCurrency => "JMD";

}

public sealed class BOMProvider : CentralBankProviderBase {
	public BOMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOM";
	public override string Name => "Bank of Mongolia";
	public override string NativeCurrency => "MNT";

}

public sealed class BOTProvider : CentralBankProviderBase {
	public BOTProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOT";
	public override string Name => "Bank of Thailand";
	public override string NativeCurrency => "THB";

}

public sealed class BOTAProvider : CentralBankProviderBase {
	public BOTAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOTA";
	public override string Name => "Bank of Tanzania";
	public override string NativeCurrency => "TZS";

}

public sealed class BRBProvider : CentralBankProviderBase {
	public BRBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BRB";
	public override string Name => "Banque de la Republique du Burundi";
	public override string NativeCurrency => "BIF";

}

public sealed class BSPProvider : CentralBankProviderBase {
	public BSPProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BSP";
	public override string Name => "Bangko Sentral ng Pilipinas";
	public override string NativeCurrency => "PHP";

}

public sealed class CBAProvider : CentralBankProviderBase {
	public CBAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBA";
	public override string Name => "Central Bank of Armenia";
	public override string NativeCurrency => "AMD";

}

public sealed class CBCProvider : CentralBankProviderBase {
	public CBCProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBC";
	public override string Name => "Central Bank of the Republic of China (Taiwan)";
	public override string NativeCurrency => "TWD";

}

public sealed class CBEProvider : CentralBankProviderBase {
	public CBEProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBE";
	public override string Name => "Central Bank of Egypt";
	public override string NativeCurrency => "EGP";

}

public sealed class CBGProvider : CentralBankProviderBase {
	public CBGProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBG";
	public override string Name => "Central Bank of The Gambia";
	public override string NativeCurrency => "GMD";

}

public sealed class CBIProvider : CentralBankProviderBase {
	public CBIProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBI";
	public override string Name => "Central Bank of Iraq";
	public override string NativeCurrency => "IQD";

}

public sealed class CBKProvider : CentralBankProviderBase {
	public CBKProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBK";
	public override string Name => "Central Bank of Kenya";
	public override string NativeCurrency => "KES";

}

public sealed class CBLLRProvider : CentralBankProviderBase {
	public CBLLRProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBLLR";
	public override string Name => "Central Bank of Liberia";
	public override string NativeCurrency => "LRD";

}

public sealed class CBMProvider : CentralBankProviderBase {
	public CBMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBM";
	public override string Name => "Central Bank of Myanmar";
	public override string NativeCurrency => "MMK";

}

public sealed class CBNProvider : CentralBankProviderBase {
	public CBNProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBN";
	public override string Name => "Central Bank of Nigeria";
	public override string NativeCurrency => "NGN";

}

public sealed class CBRProvider : CentralBankProviderBase {
	public CBRProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBR";
	public override string Name => "Central Bank of Russia";
	public override string NativeCurrency => "RUB";

}

public sealed class CBSProvider : CentralBankProviderBase {
	public CBSProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBS";
	public override string Name => "Central Bank of Samoa";
	public override string NativeCurrency => "WST";

}

public sealed class CBSLProvider : CentralBankProviderBase {
	public CBSLProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBSL";
	public override string Name => "Central Bank of Sri Lanka";
	public override string NativeCurrency => "LKR";

}

public sealed class CBUProvider : CentralBankProviderBase {
	public CBUProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBU";
	public override string Name => "Central Bank of Uzbekistan";
	public override string NativeCurrency => "UZS";

}

public sealed class CNBProvider : CentralBankProviderBase {
	public CNBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CNB";
	public override string Name => "Czech National Bank";
	public override string NativeCurrency => "CZK";

}

public sealed class DABProvider : CentralBankProviderBase {
	public DABProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "DAB";
	public override string Name => "Da Afghanistan Bank";
	public override string NativeCurrency => "AFN";

}

public sealed class DNBProvider : CentralBankProviderBase {
	public DNBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "DNB";
	public override string Name => "Danmarks Nationalbank";
	public override string NativeCurrency => "DKK";

}

public sealed class ECBProvider : CentralBankProviderBase {
	public ECBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "ECB";
	public override string Name => "European Central Bank";
	public override string NativeCurrency => "EUR";

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(DateOnly date, CancellationToken cancellationToken) {
		var url =
			"https://data-api.ecb.europa.eu/service/data/EXR/D..EUR.SP00.A" +
			$"?startPeriod={date:yyyy-MM-dd}&endPeriod={date:yyyy-MM-dd}&format=csvdata";

		var csv = await Http.GetStringAsync(url, cancellationToken);
		var rates = new List<ExchangeRate>();

		foreach (var line in csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1)) {
			var cols = SplitCsv(line);
			if (cols.Count < 2) {
				continue;
			}

			var currency = cols.FirstOrDefault(x => x.Length == 3 && x.All(char.IsLetter) && x != "EUR");
			var numeric = cols.LastOrDefault(x => decimal.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture, out _));

			if (currency is null || numeric is null) {
				continue;
			}

			if (decimal.TryParse(numeric, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) && rate > 0) {
				rates.Add(new ExchangeRate(date, "EUR", currency.ToUpperInvariant(), rate, Code));
			}
		}

		return rates;
	}

}

public sealed class FBILProvider : CentralBankProviderBase {
	public FBILProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "FBIL";
	public override string Name => "Financial Benchmarks India";
	public override string NativeCurrency => "INR";

}

public sealed class FREDProvider : CentralBankProviderBase {
	public FREDProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "FRED";
	public override string Name => "Federal Reserve Bank of St. Louis";
	public override string NativeCurrency => "USD";

}

public sealed class HKMAProvider : CentralBankProviderBase {
	public HKMAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "HKMA";
	public override string Name => "Hong Kong Monetary Authority";
	public override string NativeCurrency => "HKD";

}

public sealed class HNBProvider : CentralBankProviderBase {
	public HNBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "HNB";
	public override string Name => "Hrvatska Narodna Banka";
	public override string NativeCurrency => "EUR";

}

public sealed class IMFProvider : CentralBankProviderBase {
	public IMFProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "IMF";
	public override string Name => "International Monetary Fund";
	public override string NativeCurrency => "XDR";

}

public sealed class LBProvider : CentralBankProviderBase {
	public LBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "LB";
	public override string Name => "Lietuvos Bankas";
	public override string NativeCurrency => "EUR";

}

public sealed class MASProvider : CentralBankProviderBase {
	public MASProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "MAS";
	public override string Name => "Monetary Authority of Singapore";
	public override string NativeCurrency => "SGD";

}

public sealed class MMAProvider : CentralBankProviderBase {
	public MMAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "MMA";
	public override string Name => "Maldives Monetary Authority";
	public override string NativeCurrency => "MVR";

}

public sealed class MNBProvider : CentralBankProviderBase {
	public MNBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "MNB";
	public override string Name => "Magyar Nemzeti Bank";
	public override string NativeCurrency => "HUF";

}

public sealed class NBProvider : CentralBankProviderBase {
	public NBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NB";
	public override string Name => "Norges Bank";
	public override string NativeCurrency => "NOK";

}

public sealed class NBCProvider : CentralBankProviderBase {
	public NBCProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBC";
	public override string Name => "National Bank of Cambodia";
	public override string NativeCurrency => "KHR";

}

public sealed class NBEProvider : CentralBankProviderBase {
	public NBEProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBE";
	public override string Name => "National Bank of Ethiopia";
	public override string NativeCurrency => "ETB";

}

public sealed class NBGProvider : CentralBankProviderBase {
	public NBGProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBG";
	public override string Name => "National Bank of Georgia";
	public override string NativeCurrency => "GEL";

}

public sealed class NBKProvider : CentralBankProviderBase {
	public NBKProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBK";
	public override string Name => "National Bank of Kazakhstan";
	public override string NativeCurrency => "KZT";

}

public sealed class NBKRProvider : CentralBankProviderBase {
	public NBKRProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBKR";
	public override string Name => "National Bank of the Kyrgyz Republic";
	public override string NativeCurrency => "KGS";

}

public sealed class NBMProvider : CentralBankProviderBase {
	public NBMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBM";
	public override string Name => "National Bank of Moldova";
	public override string NativeCurrency => "MDL";

}

public sealed class NBPProvider : CentralBankProviderBase {
	public NBPProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBP";
	public override string Name => "Narodowy Bank Polski";
	public override string NativeCurrency => "PLN";

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(DateOnly date, CancellationToken cancellationToken) {
		var url = $"https://api.nbp.pl/api/exchangerates/tables/A/{date:yyyy-MM-dd}?format=json";
		using var response = await Http.GetAsync(url, cancellationToken);

		if (response.StatusCode == System.Net.HttpStatusCode.NotFound) {
			return [];
		}

		response.EnsureSuccessStatusCode();
		using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

		var rates = new List<ExchangeRate>();
		var table = doc.RootElement[0];

		foreach (var row in table.GetProperty("rates").EnumerateArray()) {
			var code = row.GetProperty("code").GetString();
			var rate = GetDecimal(row, "mid");

			if (!string.IsNullOrWhiteSpace(code) && rate > 0) {
				rates.Add(new ExchangeRate(date, code!, "PLN", rate, Code));
			}
		}

		return rates;
	}

}

public sealed class NBRBProvider : CentralBankProviderBase {
	public NBRBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBRB";
	public override string Name => "Natsyyanalny Bank Respubliki Belarus";
	public override string NativeCurrency => "BYN";

}

public sealed class NBRMProvider : CentralBankProviderBase {
	public NBRMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBRM";
	public override string Name => "Narodna Banka na Republika Severna Makedonija";
	public override string NativeCurrency => "MKD";

}

public sealed class NBTProvider : CentralBankProviderBase {
	public NBTProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBT";
	public override string Name => "National Bank of Tajikistan";
	public override string NativeCurrency => "TJS";

}

public sealed class NBUProvider : CentralBankProviderBase {
	public NBUProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBU";
	public override string Name => "Natsionalnyi Bank Ukrainy";
	public override string NativeCurrency => "UAH";

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(DateOnly date, CancellationToken cancellationToken) {
		var url =
			"https://bank.gov.ua/NBUStatService/v1/statdirectory/exchange" +
			$"?date={date:yyyyMMdd}&json";

		using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, cancellationToken));
		var rates = new List<ExchangeRate>();

		foreach (var row in doc.RootElement.EnumerateArray()) {
			var code = row.TryGetProperty("cc", out var c) ? c.GetString() : null;
			var rate = GetDecimal(row, "rate");

			if (!string.IsNullOrWhiteSpace(code) && rate > 0) {
				rates.Add(new ExchangeRate(date, code!, "UAH", rate, Code));
			}
		}

		return rates;
	}

}

public sealed class NRBProvider : CentralBankProviderBase {
	public NRBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NRB";
	public override string Name => "Nepal Rastra Bank";
	public override string NativeCurrency => "NPR";

}

public sealed class NRBTProvider : CentralBankProviderBase {
	public NRBTProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NRBT";
	public override string Name => "National Reserve Bank of Tonga";
	public override string NativeCurrency => "TOP";

}

public sealed class RBProvider : CentralBankProviderBase {
	public RBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "RB";
	public override string Name => "Sveriges Riksbank";
	public override string NativeCurrency => "SEK";

}

public sealed class RBAProvider : CentralBankProviderBase {
	public RBAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "RBA";
	public override string Name => "Reserve Bank of Australia";
	public override string NativeCurrency => "AUD";

}

public sealed class RBFProvider : CentralBankProviderBase {
	public RBFProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "RBF";
	public override string Name => "Reserve Bank of Fiji";
	public override string NativeCurrency => "FJD";

}

public sealed class RBMProvider : CentralBankProviderBase {
	public RBMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "RBM";
	public override string Name => "Reserve Bank of Malawi";
	public override string NativeCurrency => "MWK";

}

public sealed class RBVProvider : CentralBankProviderBase {
	public RBVProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "RBV";
	public override string Name => "Reserve Bank of Vanuatu";
	public override string NativeCurrency => "VUV";

}

public sealed class SARBProvider : CentralBankProviderBase {
	public SARBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "SARB";
	public override string Name => "South African Reserve Bank";
	public override string NativeCurrency => "ZAR";

}

public sealed class SBIProvider : CentralBankProviderBase {
	public SBIProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "SBI";
	public override string Name => "Seðlabanki Íslands";
	public override string NativeCurrency => "ISK";

}

public sealed class SBPProvider : CentralBankProviderBase {
	public SBPProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "SBP";
	public override string Name => "State Bank of Pakistan";
	public override string NativeCurrency => "PKR";

}

public sealed class TCMBProvider : CentralBankProviderBase {
	public TCMBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "TCMB";
	public override string Name => "Türkiye Cumhuriyet Merkez Bankası";
	public override string NativeCurrency => "TRY";

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(DateOnly date, CancellationToken cancellationToken) {
		var url = $"https://www.tcmb.gov.tr/kurlar/{date:yyyyMM}/{date:ddMMyyyy}.xml";
		var xml = await Http.GetStringAsync(url, cancellationToken);

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
				rates.Add(new ExchangeRate(date, code, "TRY", mid / unit, Code));
			}
		}

		return rates;
	}

}


public static class CentralBankProviderRegistration {
	public static IServiceCollection AddCentralBankProviders(this IServiceCollection services) {
		services.AddTransient<AMCMProvider>();
		services.AddTransient<BAMProvider>();
		services.AddTransient<BANREPProvider>();
		services.AddTransient<BANXICOProvider>();
		services.AddTransient<BBKProvider>();
		services.AddTransient<BCBProvider>();
		services.AddTransient<BCBOProvider>();
		services.AddTransient<BCCProvider>();
		services.AddTransient<BCCHProvider>();
		services.AddTransient<BCCRProvider>();
		services.AddTransient<BCEAOProvider>();
		services.AddTransient<BCNProvider>();
		services.AddTransient<BCPProvider>();
		services.AddTransient<BCRAProvider>();
		services.AddTransient<BCTProvider>();
		services.AddTransient<BCUProvider>();
		services.AddTransient<BDIProvider>();
		services.AddTransient<BDPProvider>();
		services.AddTransient<BIProvider>();
		services.AddTransient<BNAProvider>();
		services.AddTransient<BNMProvider>();
		services.AddTransient<BNRProvider>();
		services.AddTransient<BNRRWProvider>();
		services.AddTransient<BOAProvider>();
		services.AddTransient<BOBProvider>();
		services.AddTransient<BOCProvider>();
		services.AddTransient<BOEProvider>();
		services.AddTransient<BOIProvider>();
		services.AddTransient<BOJProvider>();
		services.AddTransient<BOJAProvider>();
		services.AddTransient<BOMProvider>();
		services.AddTransient<BOTProvider>();
		services.AddTransient<BOTAProvider>();
		services.AddTransient<BRBProvider>();
		services.AddTransient<BSPProvider>();
		services.AddTransient<CBAProvider>();
		services.AddTransient<CBCProvider>();
		services.AddTransient<CBEProvider>();
		services.AddTransient<CBGProvider>();
		services.AddTransient<CBIProvider>();
		services.AddTransient<CBKProvider>();
		services.AddTransient<CBLLRProvider>();
		services.AddTransient<CBMProvider>();
		services.AddTransient<CBNProvider>();
		services.AddTransient<CBRProvider>();
		services.AddTransient<CBSProvider>();
		services.AddTransient<CBSLProvider>();
		services.AddTransient<CBUProvider>();
		services.AddTransient<CNBProvider>();
		services.AddTransient<DABProvider>();
		services.AddTransient<DNBProvider>();
		services.AddTransient<ECBProvider>();
		services.AddTransient<FBILProvider>();
		services.AddTransient<FREDProvider>();
		services.AddTransient<HKMAProvider>();
		services.AddTransient<HNBProvider>();
		services.AddTransient<IMFProvider>();
		services.AddTransient<LBProvider>();
		services.AddTransient<MASProvider>();
		services.AddTransient<MMAProvider>();
		services.AddTransient<MNBProvider>();
		services.AddTransient<NBProvider>();
		services.AddTransient<NBCProvider>();
		services.AddTransient<NBEProvider>();
		services.AddTransient<NBGProvider>();
		services.AddTransient<NBKProvider>();
		services.AddTransient<NBKRProvider>();
		services.AddTransient<NBMProvider>();
		services.AddTransient<NBPProvider>();
		services.AddTransient<NBRBProvider>();
		services.AddTransient<NBRMProvider>();
		services.AddTransient<NBTProvider>();
		services.AddTransient<NBUProvider>();
		services.AddTransient<NRBProvider>();
		services.AddTransient<NRBTProvider>();
		services.AddTransient<RBProvider>();
		services.AddTransient<RBAProvider>();
		services.AddTransient<RBFProvider>();
		services.AddTransient<RBMProvider>();
		services.AddTransient<RBVProvider>();
		services.AddTransient<SARBProvider>();
		services.AddTransient<SBIProvider>();
		services.AddTransient<SBPProvider>();
		services.AddTransient<TCMBProvider>();
		services.AddTransient<CentralBankProviderFactory>();
		return services;
	}
}

public sealed class CentralBankProviderFactory {
	private readonly IServiceProvider _services;

	public CentralBankProviderFactory(IServiceProvider services) {
		_services = services;
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
