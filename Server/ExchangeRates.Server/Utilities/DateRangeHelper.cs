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
			if (date.Day == 18 && date.Month == 4) {
				Console.WriteLine(date);
			}

			if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) {
				continue;
			}

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

	/// <summary>
	/// Returns a list of missing dates between fromDate and toDate that are not present in existingDates, excluding weekends.
	/// </summary>
	public static IReadOnlyList<DateOnly> GetMissingDates(DateOnly fromDate, DateOnly toDate, IEnumerable<DateOnly> existingDates) {
		var missing = new List<DateOnly>();
		var existing = existingDates.Where(x => x >= fromDate && x <= toDate)
													.ToHashSet();

		// Iterate through the date range and find missing dates
		for (var date = fromDate; date <= toDate; date = date.AddDays(1)) {
			if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) {
				continue;
			}

			if (!existing.Contains(date)) {
				missing.Add(date);
			}
		}

		return missing;
	}
}