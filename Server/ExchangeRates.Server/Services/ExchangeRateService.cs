using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Interfaces;
using ExchangeRates.Domain.Models;
using ExchangeRates.Domain.Utilities;
using ExchangeRates.Infrastructure.Providers;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Mappers;

namespace ExchangeRates.Server.Services;

public sealed class ExchangeRateService : IExchangeRateService {
	#region Class Fields
	private readonly ILogger<ExchangeRateService> _logger;
	private readonly IExchangeRateRepository _repository;
	private readonly CentralBankProviderFactory _providerFactory;
	private readonly string _pivotCurrency;

	// A bank that hangs would otherwise hold the request for the HttpClient default of 100s before the next bank is tried.
	private static readonly TimeSpan BankTimeout = TimeSpan.FromSeconds(20);

	private const string FixedProvider = "FIXED";
	private const int MaxPegHops = 2;
	#endregion Class Fields
	public ExchangeRateService(ILogger<ExchangeRateService> logger, IExchangeRateRepository repository, CentralBankProviderFactory providerFactory, IConfiguration configuration) {
		_logger = logger;
		_repository = repository;
		_providerFactory = providerFactory;
		_pivotCurrency = configuration["PivotCurrency"] ?? throw new InvalidOperationException("Missing PivotCurrency configuration.");

	}

	/// <summary>
	/// Gets the exchange rates for the specified base and quote currencies between the given date range.<br />
	/// Uses a bank that quotes the pair directly when there is one; otherwise crosses two rates through a common currency.
	/// Every rate fetched from a bank is stored, so later requests are served from the database.
	/// </summary>
	/// <exception cref="ArgumentException"></exception>
	public Task<IReadOnlyList<ExchangeRateResult>> GetRatesAsync(string baseCurrency, string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		if (fromDate > toDate) {
			throw new ArgumentException("fromDate cannot be greater than toDate.");
		}

		return ResolveRatesAsync(baseCurrency, quoteCurrency, fromDate, toDate, MaxPegHops, ct);
	}

	/// <param name="pegHops">How many more fixed pegs may be followed, which stops chains of pegs from recursing forever</param>
	private async Task<IReadOnlyList<ExchangeRateResult>> ResolveRatesAsync(string baseCurrency, string quoteCurrency, DateOnly fromDate, DateOnly toDate, int pegHops, CancellationToken ct) {
		if (baseCurrency == quoteCurrency) {
			return Enumerable.Range(0, toDate.DayNumber - fromDate.DayNumber + 1)
				.Select(i => new ExchangeRateResult(fromDate.AddDays(i), baseCurrency, quoteCurrency, 1m, "IDENTITY"))
				.ToList();
		}

		var direct = await GetPairRatesAsync(baseCurrency, quoteCurrency, fromDate, toDate, ct);
		if (direct is not null) {
			return direct;
		}

		var banks = await GetUsableBanksAsync(baseCurrency, quoteCurrency, ct);
		foreach (var pivot in GetPivotCurrencies(baseCurrency, quoteCurrency, banks)) {
			var crossed = await GetCrossRatesAsync(baseCurrency, quoteCurrency, pivot, fromDate, toDate, ct);
			if (crossed.Count > 0) {
				return crossed;
			}
		}

		if (pegHops > 0) {
			var pegged = await GetPeggedRatesAsync(baseCurrency, quoteCurrency, fromDate, toDate, pegHops - 1, ct);
			if (pegged.Count > 0) {
				return pegged;
			}
		}

		_logger.LogWarning("No rates found for {BaseCurrency}/{QuoteCurrency} between {FromDate} and {ToDate}.", baseCurrency, quoteCurrency, fromDate, toDate);
		return [];
	}

	/// <summary>
	/// Falls back to a fixed peg when no bank publishes a currency (e.g. USN = 1 USD, VED = 1 VES, historic currencies with a
	/// fixed conversion): converts it to the currency it is pegged on and resolves the rest of the pair from there.
	/// </summary>
	private async Task<IReadOnlyList<ExchangeRateResult>> GetPeggedRatesAsync(string baseCurrency, string quoteCurrency, DateOnly fromDate, DateOnly toDate, int pegHops, CancellationToken ct) {
		// 1 PeggedOn = Rate units of the pegged currency.
		foreach (var peg in await _repository.GetFixedRatesAsync(baseCurrency, ct)) {
			var peggedOn = peg.PeggedOnCurrency.CurrencyCode;
			var rest = await ResolveRatesAsync(peggedOn, quoteCurrency, fromDate, toDate, pegHops, ct);
			var rates = ApplyPeg(rest, peg, r => new ExchangeRateResult(r.Date, baseCurrency, quoteCurrency, r.Rate / peg.Rate, JoinProviders(FixedProvider, r.Provider)));
			if (rates.Count > 0) {
				return rates;
			}
		}

		foreach (var peg in await _repository.GetFixedRatesAsync(quoteCurrency, ct)) {
			var peggedOn = peg.PeggedOnCurrency.CurrencyCode;
			var rest = await ResolveRatesAsync(baseCurrency, peggedOn, fromDate, toDate, pegHops, ct);
			var rates = ApplyPeg(rest, peg, r => new ExchangeRateResult(r.Date, baseCurrency, quoteCurrency, r.Rate * peg.Rate, JoinProviders(r.Provider, FixedProvider)));
			if (rates.Count > 0) {
				return rates;
			}
		}

		return [];
	}

	private static List<ExchangeRateResult> ApplyPeg(IReadOnlyList<ExchangeRateResult> rates, FixedExchangeRateEntity peg, Func<ExchangeRateResult, ExchangeRateResult> convert) {
		return rates.Where(r => (peg.ValidFrom is null || r.Date >= peg.ValidFrom) && (peg.ValidTo is null || r.Date <= peg.ValidTo))
			.Select(convert)
			.ToList();
	}

	// Rates read back from the database carry no provider, so join only the ones that are known.
	private static string JoinProviders(params string?[] providers) {
		return string.Join('+', providers.Where(p => !string.IsNullOrEmpty(p) && p != "IDENTITY").Distinct());
	}

	/// <summary> Active banks that publish both currencies and have a provider implementation, by priority </summary>
	private async Task<IReadOnlyList<CentralBankEntity>> GetUsableBanksAsync(string currency1, string currency2, CancellationToken ct) {
		var banks = await _repository.GetBanksPublishingAsync(currency1, currency2, ct);
		return banks.Where(b => _providerFactory.HasProvider(b.BankCode)).ToList();
	}

	/// <summary>
	/// Currencies to cross through when no bank quotes the pair directly: first the native currencies of banks that publish
	/// both (both legs then come from the same source), then the configured pivot currency.
	/// </summary>
	private IEnumerable<string> GetPivotCurrencies(string baseCurrency, string quoteCurrency, IReadOnlyList<CentralBankEntity> banks) {
		return banks.Select(b => b.NativeCurrency.CurrencyCode)
			.Append(_pivotCurrency)
			.Where(c => c != baseCurrency && c != quoteCurrency)
			.Distinct();
	}

	/// <summary>
	/// Gets the rates of a pair that some bank quotes directly: stored rates first, then the missing dates from the
	/// candidate banks in priority order, falling back to the next bank when one fails or returns nothing.<br />
	/// Returns null when dates are missing and no bank could provide any rate, so the caller can try a cross rate.
	/// </summary>
	private async Task<IReadOnlyList<ExchangeRateResult>?> GetPairRatesAsync(string baseCurrency, string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var stored = (await _repository.GetRatesAsync(baseCurrency, quoteCurrency, fromDate, toDate, ct))
			.Select(ExchangeRateMapper.ToResult)
			.ToList();

		IReadOnlyList<DateOnly> missingDates = DateRangeHelper.GetMissingDates(fromDate, toDate, stored.Select(x => x.Date));
		if (missingDates.Count > 0) {
			var unavailable = await _repository.GetUnavailableDatesAsync(baseCurrency, quoteCurrency, missingDates.Min(), missingDates.Max(), ct);
			missingDates = missingDates.Where(x => !unavailable.Contains(x)).ToList();
		}

		if (missingDates.Count == 0) {
			return OrientRates(stored, baseCurrency, quoteCurrency);
		}

		var fetchFrom = missingDates.Min();
		var fetchTo = missingDates.Max();
		var banks = await GetUsableBanksAsync(baseCurrency, quoteCurrency, ct);
		var directBanks = banks.Where(b => b.NativeCurrency.CurrencyCode == baseCurrency || b.NativeCurrency.CurrencyCode == quoteCurrency);

		foreach (var bank in directBanks) {
			IReadOnlyList<ExchangeRateResult> fetched;
			try {
				using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
				timeout.CancelAfter(BankTimeout);
				fetched = await FetchFromBankAsync(bank, baseCurrency, quoteCurrency, fetchFrom, fetchTo, timeout.Token);
			}
			catch (Exception ex) when (!ct.IsCancellationRequested) {
				_logger.LogWarning(ex, "Bank {BankCode} failed for {BaseCurrency}/{QuoteCurrency}; trying the next one.", bank.BankCode, baseCurrency, quoteCurrency);
				continue;
			}

			if (fetched.Count == 0) {
				_logger.LogInformation("Bank {BankCode} returned no rates for {BaseCurrency}/{QuoteCurrency} between {FromDate} and {ToDate}.", bank.BankCode, baseCurrency, quoteCurrency, fetchFrom, fetchTo);
				continue;
			}

			await SaveRatesAsync(bank, baseCurrency, quoteCurrency, fetchFrom, fetchTo, fetched, ct);

			var storedDates = stored.Select(x => x.Date).ToHashSet();
			var merged = stored.Concat(fetched.Where(x => !storedDates.Contains(x.Date) && x.Date >= fromDate && x.Date <= toDate))
				.OrderBy(x => x.Date)
				.ToList();
			return OrientRates(merged, baseCurrency, quoteCurrency);
		}

		return stored.Count > 0 ? OrientRates(stored, baseCurrency, quoteCurrency) : null;
	}

	/// <summary>
	/// Gets the rates from the bank API for the specified base and quote currencies between the given date range. <br />
	/// The bank's native currency must be one of the two; the result is oriented as native/other.
	/// </summary>
	private async Task<IReadOnlyList<ExchangeRateResult>> FetchFromBankAsync(CentralBankEntity bank, string baseCurrency, string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var provider = _providerFactory.GetProvider(bank);
		var localQuoteCurrency = (provider.NativeCurrencyCode == quoteCurrency) ? baseCurrency : quoteCurrency;

		_logger.LogDebug("Fetching {BaseCurrency}/{QuoteCurrency} from {BankCode}. Native={NativeCurrency}", baseCurrency, quoteCurrency, provider.BankCode, provider.NativeCurrencyCode);

		var rates = await provider.GetRatesAsync(localQuoteCurrency, fromDate, toDate, ct);

		return rates.Where(x => x.Rate > 0)
			.Select(x => new ExchangeRateResult(x.Date, provider.NativeCurrencyCode, localQuoteCurrency, x.Rate, provider.BankCode))
			.OrderBy(x => x.Date)
			.ToList();
	}

	/// <summary>
	/// Stores fetched rates under their canonical currency order, and records the dates the bank had no rate for so they are
	/// not requested again. Dates after the last returned one are not recorded: the bank may simply not have published them yet.
	/// </summary>
	private async Task SaveRatesAsync(CentralBankEntity bank, string baseCurrency, string quoteCurrency, DateOnly fromDate, DateOnly toDate, IReadOnlyList<ExchangeRateResult> rates, CancellationToken ct) {
		var rateDates = rates.Select(x => x.Date).ToHashSet();
		var lastPublished = rates.Max(x => x.Date);
		var unavailable = new List<ExchangeRateUnavailableDateEntity>();

		for (var date = fromDate; date <= toDate && date < lastPublished; date = date.AddDays(1)) {
			if (!rateDates.Contains(date)) {
				unavailable.Add(new ExchangeRateUnavailableDateEntity {
					CentralBankId = bank.Id,
					BaseCurrency = baseCurrency,
					QuoteCurrency = quoteCurrency,
					UnavailableDate = date,
					Reason = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday ? "Weekend" : "Possible holiday",
				});
			}
		}
		await _repository.AddUnavailableDatesAsync(unavailable, ct);

		var canonical = await OrientToCanonical(rates, ct);
		var currencyIds = (await _repository.GetCurrencyByCodesAsync([baseCurrency, quoteCurrency], ct))
			.ToDictionary(x => x.CurrencyCode, x => x.Id);

		// OrientToCanonical may have swapped base and quote (and inverted the rate), so store each row under its own currencies.
		var saved = await _repository.AddRangeAsync(canonical.Select(x => ExchangeRateMapper.ToEntity(x, currencyIds[x.BaseCurrency], currencyIds[x.QuoteCurrency])), ct);

		_logger.LogInformation("Saved {Count} new rates for {BaseCurrency}/{QuoteCurrency} from {BankCode} between {FromDate} and {ToDate}.", saved, baseCurrency, quoteCurrency, bank.BankCode, fromDate, toDate);
	}

	/// <summary>
	/// Crosses base/pivot and pivot/quote into base/quote for the dates both legs have. Each leg is a directly quoted pair,
	/// so it is read from and stored in the database like any other.
	/// </summary>
	private async Task<IReadOnlyList<ExchangeRateResult>> GetCrossRatesAsync(string baseCurrency, string quoteCurrency, string pivot, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var baseLeg = await GetPairRatesAsync(baseCurrency, pivot, fromDate, toDate, ct);
		if (baseLeg is not { Count: > 0 }) {
			return [];
		}

		var quoteLeg = await GetPairRatesAsync(pivot, quoteCurrency, fromDate, toDate, ct);
		if (quoteLeg is not { Count: > 0 }) {
			return [];
		}

		_logger.LogInformation("Crossing {BaseCurrency}/{QuoteCurrency} through {Pivot}.", baseCurrency, quoteCurrency, pivot);

		var quoteByDate = quoteLeg.GroupBy(x => x.Date).ToDictionary(x => x.Key, x => x.First());

		return baseLeg.GroupBy(x => x.Date)
			.Select(x => x.First())
			.Where(x => quoteByDate.ContainsKey(x.Date))
			.Select(x => {
				var quoteRate = quoteByDate[x.Date];
				return new ExchangeRateResult(x.Date, baseCurrency, quoteCurrency, x.Rate * quoteRate.Rate, JoinProviders(x.Provider, quoteRate.Provider));
			})
			.OrderBy(x => x.Date)
			.ToList();
	}


	private IReadOnlyList<ExchangeRateResult> OrientRates(IReadOnlyList<ExchangeRateResult> canonicalRates, string baseCurrency, string quoteCurrency) {
		if (canonicalRates.Count == 0 ||
			(canonicalRates[0].BaseCurrency == baseCurrency && canonicalRates[0].QuoteCurrency == quoteCurrency)) {
			return canonicalRates;
		}

		return canonicalRates.Where(x => x.Rate != 0)
											.Select(x => {
												if (x.BaseCurrency == baseCurrency && x.QuoteCurrency == quoteCurrency) {
													return x;
												}

												if (x.BaseCurrency == quoteCurrency && x.QuoteCurrency == baseCurrency) {
													return new ExchangeRateResult(x.Date, baseCurrency, quoteCurrency, 1m / x.Rate, x.Provider);
												}

												throw new InvalidOperationException($"Rate {x.BaseCurrency}/{x.QuoteCurrency} cannot be oriented as {baseCurrency}/{quoteCurrency}.");
											})
											.ToList();
	}

	/// <summary>
	/// Orients the exchange rates to a canonical form based on the priority of the currencies. <br />
	/// </summary>
	private async Task<List<ExchangeRateResult>> OrientToCanonical(IEnumerable<ExchangeRateResult> rates, CancellationToken ct) {
		var currencyCodes = rates.SelectMany(x => new[] { x.BaseCurrency, x.QuoteCurrency }).Distinct();
		var currencies = await _repository.GetCurrencyByCodesAsync(currencyCodes, ct);

		return rates.Where(x => x.Rate != 0)
			.Select(x => {
				var baseCurrency = currencies.First(c => c.CurrencyCode == x.BaseCurrency);
				var quoteCurrency = currencies.First(c => c.CurrencyCode == x.QuoteCurrency);

				if (baseCurrency.Priority < quoteCurrency.Priority) {
					return x;
				}

				return new ExchangeRateResult(x.Date, x.QuoteCurrency, x.BaseCurrency, 1m / x.Rate, x.Provider);
			})
			.OrderBy(x => x.Date)
			.ToList();
	}

	public async Task<IReadOnlyList<CurrencyModel>> GetCurrenciesAsync(CancellationToken ct) {
		var currencies = await _repository.GetCurrenciesAsync(ct);

		var result = currencies.Select(x => new CurrencyModel(x.CurrencyCode, x.NumericCode, x.Name, x.IsHistoric)).ToList();
		return result;
	}

	/// <summary> Gets the list of central banks from the repository </summary>
	public async Task<IReadOnlyList<CentralBankModel>> GetCentralBanksAsync(CancellationToken ct) {
		var centralBanks = await _repository.GetCentralBanksAsync(ct);
		return centralBanks.Select(x => new CentralBankModel(x.BankCode, x.BankName, x.CountryOfOrigin, x.NativeCurrency.CurrencyCode, x.CurrencyId, x.Priority))
										.ToList();
	}

}