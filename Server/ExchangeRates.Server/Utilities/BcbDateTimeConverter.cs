namespace ExchangeRates.Server.Utils;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

public class BcbDateTimeConverter : JsonConverter<DateTime> {
	public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
		var value = reader.GetString();

		if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)) {
			return result;
		}

		throw new JsonException($"Invalid BCB DateTime: {value}");
	}

	public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) {
		writer.WriteStringValue(value);
	}
}