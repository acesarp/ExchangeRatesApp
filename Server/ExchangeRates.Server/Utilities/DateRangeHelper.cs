using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Utilities;

public static class DateRangeHelper {
	public static IReadOnlyList<(DateOnly From, DateOnly To)> GetMissingRanges(DateOnly fromDate, DateOnly toDate, IReadOnlyList<ExchangeRateFetch> fetches) {
		var missing = new List<(DateOnly From, DateOnly To)>();
		var current = fromDate;

		foreach (var fetch in fetches.OrderBy(x => x.FromDate)) {
			if (fetch.ToDate < current) {
				continue;
			}

			if (fetch.FromDate > toDate) {
				break;
			}

			if (fetch.FromDate > current) {
				missing.Add((current, fetch.FromDate.AddDays(-1)));
			}

			if (fetch.ToDate >= current) {
				current = fetch.ToDate.AddDays(1);
			}

			if (current > toDate) {
				break;
			}
		}

		if (current <= toDate) {
			missing.Add((current, toDate));
		}

		return missing;
	}
}