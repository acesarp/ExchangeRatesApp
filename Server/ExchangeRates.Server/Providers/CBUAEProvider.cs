
using ExchangeRates.Domain.Entities;

using HtmlAgilityPack;

using System.Globalization;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of the United Arab Emirates
/// </summary>
public sealed class CBUAEProvider : CentralBankProviderBase {
	private readonly ILogger<CBUAEProvider> _logger;

	public CBUAEProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<CBUAEProvider> logger) : base(http, configuration, bank) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		if (!CurrencyMap.TryGetValue(quoteCurrency, out var currencyName)) {
			_logger.LogDebug("Currency {Currency} is not supported by CBUAE.", quoteCurrency);
			return [];
		}

		var results = new List<ExchangeRateResult>();

		for (var date = fromDate; date <= toDate; date = date.AddDays(1)) {
			var result = await FetchDateAsync(date, quoteCurrency, currencyName, ct);

			if (result is not null) {
				results.Add(result);
			}
		}

		return results;
	}

	private async Task<ExchangeRateResult?> FetchDateAsync(DateOnly date, string quoteCurrency, string currencyName, CancellationToken ct) {

		var url = $"{Url.TrimEnd('/')}/GetExchangeRateAllCurrencyDate?dateTime={date:yyyy-MM-dd}";

		using var request = new HttpRequestMessage(HttpMethod.Get, url);

		request.Headers.Referrer = new Uri(Url);
		request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");

		using var response = await Http.SendAsync(request, ct);

		if (!response.IsSuccessStatusCode) {
			_logger.LogWarning("CBUAE returned {StatusCode} for {Date}.", response.StatusCode, date);

			return null;
		}

		var html = await response.Content.ReadAsStringAsync(ct);

		var document = new HtmlDocument();
		document.LoadHtml(html);

		var rows = document.DocumentNode.SelectNodes("//tr");

		if (rows is null) {
			return null;
		}

		foreach (var row in rows) {
			var cells = row.SelectNodes("./td");

			if (cells is null || cells.Count < 2) {
				continue;
			}

			var name = Clean(cells[^2].InnerText);
			var rateText = Clean(cells[^1].InnerText);

			if (!name.Equals(currencyName, StringComparison.OrdinalIgnoreCase)) {
				continue;
			}

			if (!decimal.TryParse(rateText, NumberStyles.Float, CultureInfo.InvariantCulture, out var rate)) {
				continue;
			}

			return new ExchangeRateResult(date, Bank.Currency.CurrencyCode, quoteCurrency, rate, Bank.BankCode);
		}

		return null;
	}

	private static string Clean(string value) {
		return string.Join(" ",
			HtmlEntity.DeEntitize(value)
				.Replace("\r", " ")
				.Replace("\n", " ")
				.Replace("\t", " ")
				.Split(' ', StringSplitOptions.RemoveEmptyEntries));
	}

	private static readonly IReadOnlyDictionary<string, string> CurrencyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
		["دولار امريكي"] = "USD",
		["بيسو ارجنتيني"] = "ARS",
		["دولار استرالي"] = "AUD",
		["تاكا بنغلاديشية"] = "BDT",
		["دينار بحريني"] = "BHD",
		["دولار بروناي"] = "BND",
		["ريال برازيلي"] = "BRL",
		["بولا بوتسواني"] = "BWP",
		["روبل بلاروسي"] = "BYN",
		["دولار كندي"] = "CAD",
		["فرنك سويسري"] = "CHF",
		["بيزو تشيلي"] = "CLP",

		// Offshore Chinese Yuan (CNH). Use CNY if CNH does not exist in ECurrencyISO.
		["يوان صيني - الخارج"] = "CNY",
		["يوان صيني"] = "CNY",

		["بيزو كولومبي"] = "COP",
		["كرونة تشيكية"] = "CZK",
		["كرون دانماركي"] = "DKK",
		["دينار جزائري"] = "DZD",
		["جينيه مصري"] = "EGP",
		["يورو"] = "EUR",
		["جنيه استرليني"] = "GBP",
		["دولار هونج كونج"] = "HKD",
		["فورنت هنغاري"] = "HUF",
		["روبية اندونيسية"] = "IDR",
		["روبية هندية"] = "INR",
		["كرونة آيسلندية"] = "ISK",
		["دينار أردني"] = "JOD",
		["ين ياباني"] = "JPY",
		["شلن كيني"] = "KES",
		["ون كوري"] = "KRW",
		["دينار كويتي"] = "KWD",
		["تينغ كازاخستاني"] = "KZT",
		["ليرة لبنانية"] = "LBP",
		["روبية سريلانكي"] = "LKR",
		["درهم مغربي"] = "MAD",
		["دينار مقدوني"] = "MKD",
		["بيسو مكسيكي"] = "MXN",
		["رينغيت ماليزي"] = "MYR",
		["نيرا نيجيري"] = "NGN",
		["كرون نرويجي"] = "NOK",
		["دولار نيوزيلندي"] = "NZD",
		["ريال عماني"] = "OMR",
		["سول بيروفي"] = "PEN",
		["بيسو فلبيني"] = "PHP",
		["روبية باكستانية"] = "PKR",
		["زلوتي بولندي"] = "PLN",
		["ريال قطري"] = "QAR",
		["دينار صربي"] = "RSD",
		["روبل روسي"] = "RUB",
		["ريال سعودي"] = "SAR",
		["دينار سوداني"] = "SDG",
		["كرونة سويدية"] = "SEK",
		["دولار سنغافوري"] = "SGD",
		["بات تايلندي"] = "THB",
		["دينار تونسي"] = "TND",
		["ليرة تركية"] = "TRY",
		["دولار تريندادي"] = "TTD",
		["دولار تايواني"] = "TWD",
		["شلن تنزاني"] = "TZS",
		["شلن اوغندي"] = "UGX",
		["دونغ فيتنامي"] = "VND",
		["ريال يمني"] = "YER",
		["راند جنوب أفريقي"] = "ZAR",
		["كواشا زامبي"] = "ZMW",
		["مانات أذربيجاني"] = "AZN",
		["ليف بلغاري"] = "BGN",
		["بر إثيوبي"] = "ETB",
		["دينار عراقي"] = "IQD",
		["شيكل اسرائيلي"] = "ILS",
		["دينار ليبي"] = "LYD",
		["روبي موريشي"] = "MUR",
		["روبية نيبالية"] = "NPR",
		["ليو روماني"] = "RON",
		["ليرة سورية"] = "SYP",
		["منات تركمانستاني"] = "TMT",
		["سوم أوزبكستاني"] = "UZS",
		["ريال ايراني"] = "IRR"
	};

}
