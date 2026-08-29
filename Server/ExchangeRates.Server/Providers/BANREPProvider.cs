using ExchangeRates.Domain.Enums;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco de la República
/// </summary>
public sealed class BANREPProvider : CentralBankProviderBase {
	private readonly ILogger<BANREPProvider> _logger;
	public BANREPProvider(HttpClient http, IConfiguration configuration, ILogger<BANREPProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BANREP";
	public override string Name => "Banco de la República";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.COP;
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		var url = $"{Url}?$where=vigenciadesde >= '{fromDate}T00:00:00.000' AND vigenciadesde < '{toDate}T00:00:00.000'";

		using var response = await Http.GetAsync(url, ct);
		response.EnsureSuccessStatusCode();

		using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

		if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0) {
			return [];
		}

		var row = doc.RootElement[0];
		var rate = GetDecimal(row, "valor");

		if (rate <= 0) {
			return [];
		}
		return [new ExchangeRateResult(fromDate, NativeCurrency, quoteCurrency, rate, Code)];
	}
}
