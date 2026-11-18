namespace ExchangeRates.Infrastructure;

public sealed class AccessLogEntity {
	public long Id { get; set; }
	public DateTime TimestampUtc { get; set; }
	public string? UserId { get; set; }      // Entra "oid"
	public string? UserName { get; set; }
	public string? Email { get; set; }
	public string? IpAddress { get; set; }
	public string Method { get; set; } = "";
	public string Path { get; set; } = "";
	public string? QueryString { get; set; }
	public int StatusCode { get; set; }
	public int DurationMs { get; set; }
	public string? UserAgent { get; set; }
	public string? Country { get; set; }   // "CA"
	public string? Region { get; set; }    // "British Columbia"
	public string? City { get; set; }      // "Vancouver"
	public double? Latitude { get; set; }
	public double? Longitude { get; set; }
	public string? Browser { get; set; }
	public string? BrowserVersion { get; set; }
	public string? OS { get; set; }
	public string? Device { get; set; }
	public bool? IsBot { get; set; }
}
