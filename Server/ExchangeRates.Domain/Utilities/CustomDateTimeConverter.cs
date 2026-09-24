namespace ExchangeRates.Domain.Utilities;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

public class CustomDateTimeConverter : JsonConverter<DateTime> {

	/// <summary>
	/// Reads a DateTime value from JSON using the specified Utf8JsonReader, type to convert, and JsonSerializerOptions.
	/// </summary>
	/// <exception cref="JsonException"></exception>
	public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
		var value = reader.GetString();

		if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)) {
			return result;
		}

		throw new JsonException($"Invalid DateTime: {value}");
	}

	/// <summary>
	/// Writes a DateTime value to JSON using the specified Utf8JsonWriter, value to write, and JsonSerializerOptions.
	/// </summary>
	public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) {
		writer.WriteStringValue(value);
	}
}