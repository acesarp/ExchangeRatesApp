namespace ExchangeRates.Server.Utilities;

public sealed class TextUtils {
	public static List<string> SplitCsv(string line) {
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
