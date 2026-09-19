using ExcelDataReader;

using ExchangeRates.Domain.Entities;

using System.Globalization;
using System.Text;

namespace ExchangeRates.Server.Providers;

/// <summary> Saudi Central Bank </summary>
public sealed class SAMAProvider : CentralBankProviderBase {
	private readonly ILogger<SAMAProvider> _logger;

	private static readonly IReadOnlyDictionary<string, string> CurrencyMap = new Dictionary<string, string> {
		["يورو"] = "EUR",
		["جنية استرليني"] = "GBP",
		["فرنك سويسري"] = "CHF",
		["ين ياباني"] = "JPY",
		["يوان رومب صيني"] = "CNY",
		["روبية باكستانية"] = "PKR",
		["روبية هندية"] = "INR",
		["ريال قطري"] = "QAR",
		["دينار كويتي"] = "KWD",
		["ريال عماني"] = "OMR",
		["دينار بحريني"] = "BHD",
		["دينار اردني"] = "JOD",
		["ماركا بوسنة وهرسك"] = "BAM",
		["تاكا بنغلادشية"] = "BDT",
		["ليف بلغاري"] = "BGN",
		["دولار بروناي"] = "BND",
		["ريال برازيلي"] = "BRL",
		["دولار كندي"] = "CAD",
		["ليك الباني"] = "ALL",
		["بيسو كوبي"] = "CUP",
		["جنيه قبرصي"] = "CYP",
		["كرونا تشيكي"] = "CZK",
		["فرنك جيبوتي"] = "DJF",
		["كرون دنماركي"] = "DKK",
		["دينار جزائري"] = "DZD",
		["جنيه مصري"] = "EGP",
		["بير اثيوبي"] = "ETB",
		["ستان افغاني"] = "AFN",
		["بيزو ارجنتيني"] = "ARS",

		["فرنك غيني"] = "GNF",
		["دولار هونج كونج"] = "HKD",
		["درهم اماراتي"] = "AED",
		["روبية اندونيسية"] = "IDR",
		["كرونا أيسلندي"] = "ISK",
		["دولار استرالي"] = "AUD",
		["شلن كيني"] = "KES",
		["ون كوري جنوبي"] = "KRW",
		["ليرة لبنانية"] = "LBP",
		["روبية سريلانكية"] = "LKR",
		["درهم مغربي"] = "MAD",
		["فرنك مدغشقر"] = "MGA",
		["كيات ماينمار"] = "MMK",
		["ليرة مالطي"] = "MTL",
		["روبية مورسيه"] = "MUR",
		["بيسو مكسيكي"] = "MXN",
		["رانجيت ماليزي"] = "MYR",
		["نيرا نيجيري"] = "NGN",
		["كرون نرويجي"] = "NOK",
		["دولار نيوزلندي"] = "NZD",
		["بيسو فلبيني"] = "PHP",
		["زلوني بولندي"] = "PLN",
		["ليو روماني"] = "RON",
		["روبل روسي"] = "RUB",
		["حقوق السحب الخاصة (سلة عملات)"] = "XDR",
		["كرون سويدي"] = "SEK",
		["دولار سنغافوري"] = "SGD",
		["كرونا سلوفاكي"] = "SKK",
		["شلن صومالي"] = "SOS",

		["بات تايلندي"] = "THB",
		["سموني طاجاكستاني"] = "TJS",
		["دينار تونسي"] = "TND",
		["ليرة تركية"] = "TRY",
		["دولار تايوان جديد"] = "TWD",
		["شلن تنزاني"] = "TZS",
		["شلن اوغندي"] = "UGX",
		["دنق فيتنامي"] = "VND",
		["فرنك كاميروني"] = "XAF",
		["فرنك النيجر"] = "XOF",
		["ريال يمني"] = "YER",
		["رند جنوب افريقيا"] = "ZAR",
		["فورنت هنغاري"] = "HUF"
	};

	public SAMAProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<SAMAProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();

		for (var month = new DateOnly(fromDate.Year, fromDate.Month, 1); month <= toDate; month = month.AddMonths(1)) {
			var rate = await FetchMonthAsync(quoteCurrency, month, ct);
			if (rate is null) {
				continue;
			}

			var start = month.Year == fromDate.Year && month.Month == fromDate.Month ? fromDate : month;
			var endOfMonth = month.AddMonths(1).AddDays(-1);
			var end = month.Year == toDate.Year && month.Month == toDate.Month ? toDate : endOfMonth;

			for (var date = start; date <= end; date = date.AddDays(1)) {
				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate.Value, Bank.BankCode));
			}
		}

		return results;
	}

	private async Task<decimal?> FetchMonthAsync(string quoteCurrency, DateOnly month, CancellationToken ct) {
		var monthName = month.ToString("MMMM", CultureInfo.InvariantCulture);
		var fileName = Uri.EscapeDataString($"{(monthName == "August" ? "Aug" : monthName)} {month.Year}") + ".xls";
		var url = $"{ApiUrl.TrimEnd('/')}/{fileName}";

		_logger.LogDebug("Downloading SAMA exchange-rate file {ApiUrl}", url);

		using var response = await Http.GetAsync(url, ct);
		if (!response.IsSuccessStatusCode) {
			_logger.LogWarning("SAMA exchange-rate file unavailable for {Year}-{Month:00}: {StatusCode}", month.Year, month.Month, response.StatusCode);
			return null;
		}

		await using var stream = await response.Content.ReadAsStreamAsync(ct);
		using var reader = ExcelReaderFactory.CreateReader(stream);
		var rowNumber = 0;
		do {
			while (reader.Read()) {
				rowNumber++;
				if (rowNumber < 12) {
					continue;
				}

				for (var column = 1; column + 1 < reader.FieldCount; column += 2) {
					var val = reader.GetValue(column);
					var currencyName = Normalize(val);
					if (currencyName is null || !CurrencyMap.TryGetValue(currencyName, out var currencyCode) || !currencyCode.Equals(quoteCurrency, StringComparison.OrdinalIgnoreCase)) {
						continue;
					}

					if (!TryGetDecimal(reader.GetValue(column + 1), out var sarPerCurrency) || sarPerCurrency <= 0) {
						continue;
					}

					// SAMA publishes SAR per unit of foreign currency.
					// Example: 1 USD = 3.75 SAR, therefore 1 SAR = 1 / 3.75 USD.
					return 1m / sarPerCurrency;
				}
			}
		} while (reader.NextResult());

		return null;
	}

	private static string? Normalize(object? value) {
		var text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim();
		return string.IsNullOrWhiteSpace(text) ? null : text.Replace('\u00A0', ' ').Trim();
	}

	private static bool TryGetDecimal(object? value, out decimal result) {
		if (value is double d) {
			result = Convert.ToDecimal(d);
			return true;
		}

		return decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
	}
}