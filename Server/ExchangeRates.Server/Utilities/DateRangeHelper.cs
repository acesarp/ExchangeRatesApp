namespace ExchangeRates.Server.Utilities;

public static class DateRangeHelper {
	public static IReadOnlyList<(DateOnly From, DateOnly To)> GetMissingRanges(DateOnly fromDate, DateOnly toDate, IReadOnlyCollection<DateOnly> existingDates) {
		var missing = new List<(DateOnly From, DateOnly To)>();
		var current = fromDate;
		if (fromDate > toDate) {
			return missing;
		}
		var existing = existingDates.Where(x => x >= fromDate && x <= toDate)
													.ToHashSet();

		DateOnly? rangeStart = null;

		for (var date = fromDate; date <= toDate; date = date.AddDays(1)) {
			if (!existing.Contains(date)) {
				rangeStart ??= date;
				continue;
			}

			if (rangeStart.HasValue) {
				missing.Add((rangeStart.Value, date.AddDays(-1)));
				rangeStart = null;
			}
		}
		if (rangeStart.HasValue) {
			missing.Add((rangeStart.Value, toDate));
		}

		return missing;
	}
}