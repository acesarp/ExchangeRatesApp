using ExchangeRates.Domain.Enums;

using HtmlAgilityPack;

using System.Globalization;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of the United Arab Emirates
/// </summary>
public sealed class CBUAEProvider : CentralBankProviderBase {
	private readonly ILogger<CBUAEProvider> _logger;

	public CBUAEProvider(HttpClient http, IConfiguration configuration, ILogger<CBUAEProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "CBUAE";
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		if (!CurrencyCountryMap.CurrencyNames.TryGetValue(quoteCurrency, out var currencyName)) {
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

	private async Task<ExchangeRateResult?> FetchDateAsync(DateOnly date, ECurrencyISO quoteCurrency, string currencyName, CancellationToken ct) {

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

			if (InverseProvider) {
				rate = 1m / rate;
			}
			return new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code);
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

	private static readonly IReadOnlyDictionary<string, ECurrencyISO> CurrencyMap = new Dictionary<string, ECurrencyISO>(StringComparer.OrdinalIgnoreCase) {
		["دولار امريكي"] = ECurrencyISO.USD,
		["بيسو ارجنتيني"] = ECurrencyISO.ARS,
		["دولار استرالي"] = ECurrencyISO.AUD,
		["تاكا بنغلاديشية"] = ECurrencyISO.BDT,
		["دينار بحريني"] = ECurrencyISO.BHD,
		["دولار بروناي"] = ECurrencyISO.BND,
		["ريال برازيلي"] = ECurrencyISO.BRL,
		["بولا بوتسواني"] = ECurrencyISO.BWP,
		["روبل بلاروسي"] = ECurrencyISO.BYN,
		["دولار كندي"] = ECurrencyISO.CAD,
		["فرنك سويسري"] = ECurrencyISO.CHF,
		["بيزو تشيلي"] = ECurrencyISO.CLP,

		// Offshore Chinese Yuan (CNH). Use CNY if CNH does not exist in ECurrencyISO.
		["يوان صيني - الخارج"] = ECurrencyISO.CNY,
		["يوان صيني"] = ECurrencyISO.CNY,

		["بيزو كولومبي"] = ECurrencyISO.COP,
		["كرونة تشيكية"] = ECurrencyISO.CZK,
		["كرون دانماركي"] = ECurrencyISO.DKK,
		["دينار جزائري"] = ECurrencyISO.DZD,
		["جينيه مصري"] = ECurrencyISO.EGP,
		["يورو"] = ECurrencyISO.EUR,
		["جنيه استرليني"] = ECurrencyISO.GBP,
		["دولار هونج كونج"] = ECurrencyISO.HKD,
		["فورنت هنغاري"] = ECurrencyISO.HUF,
		["روبية اندونيسية"] = ECurrencyISO.IDR,
		["روبية هندية"] = ECurrencyISO.INR,
		["كرونة آيسلندية"] = ECurrencyISO.ISK,
		["دينار أردني"] = ECurrencyISO.JOD,
		["ين ياباني"] = ECurrencyISO.JPY,
		["شلن كيني"] = ECurrencyISO.KES,
		["ون كوري"] = ECurrencyISO.KRW,
		["دينار كويتي"] = ECurrencyISO.KWD,
		["تينغ كازاخستاني"] = ECurrencyISO.KZT,
		["ليرة لبنانية"] = ECurrencyISO.LBP,
		["روبية سريلانكي"] = ECurrencyISO.LKR,
		["درهم مغربي"] = ECurrencyISO.MAD,
		["دينار مقدوني"] = ECurrencyISO.MKD,
		["بيسو مكسيكي"] = ECurrencyISO.MXN,
		["رينغيت ماليزي"] = ECurrencyISO.MYR,
		["نيرا نيجيري"] = ECurrencyISO.NGN,
		["كرون نرويجي"] = ECurrencyISO.NOK,
		["دولار نيوزيلندي"] = ECurrencyISO.NZD,
		["ريال عماني"] = ECurrencyISO.OMR,
		["سول بيروفي"] = ECurrencyISO.PEN,
		["بيسو فلبيني"] = ECurrencyISO.PHP,
		["روبية باكستانية"] = ECurrencyISO.PKR,
		["زلوتي بولندي"] = ECurrencyISO.PLN,
		["ريال قطري"] = ECurrencyISO.QAR,
		["دينار صربي"] = ECurrencyISO.RSD,
		["روبل روسي"] = ECurrencyISO.RUB,
		["ريال سعودي"] = ECurrencyISO.SAR,
		["دينار سوداني"] = ECurrencyISO.SDG,
		["كرونة سويدية"] = ECurrencyISO.SEK,
		["دولار سنغافوري"] = ECurrencyISO.SGD,
		["بات تايلندي"] = ECurrencyISO.THB,
		["دينار تونسي"] = ECurrencyISO.TND,
		["ليرة تركية"] = ECurrencyISO.TRY,
		["دولار تريندادي"] = ECurrencyISO.TTD,
		["دولار تايواني"] = ECurrencyISO.TWD,
		["شلن تنزاني"] = ECurrencyISO.TZS,
		["شلن اوغندي"] = ECurrencyISO.UGX,
		["دونغ فيتنامي"] = ECurrencyISO.VND,
		["ريال يمني"] = ECurrencyISO.YER,
		["راند جنوب أفريقي"] = ECurrencyISO.ZAR,
		["كواشا زامبي"] = ECurrencyISO.ZMW,
		["مانات أذربيجاني"] = ECurrencyISO.AZN,
		["ليف بلغاري"] = ECurrencyISO.BGN,
		["بر إثيوبي"] = ECurrencyISO.ETB,
		["دينار عراقي"] = ECurrencyISO.IQD,
		["شيكل اسرائيلي"] = ECurrencyISO.ILS,
		["دينار ليبي"] = ECurrencyISO.LYD,
		["روبي موريشي"] = ECurrencyISO.MUR,
		["روبية نيبالية"] = ECurrencyISO.NPR,
		["ليو روماني"] = ECurrencyISO.RON,
		["ليرة سورية"] = ECurrencyISO.SYP,
		["منات تركمانستاني"] = ECurrencyISO.TMT,
		["سوم أوزبكستاني"] = ECurrencyISO.UZS,
		["ريال ايراني"] = ECurrencyISO.IRR
	};

}