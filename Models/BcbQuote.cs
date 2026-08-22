namespace ExchangeRates.Server.Models;

public class BcbQuote {

	public decimal ParidadeCompra { get; set; }
	public decimal ParidadeVenda { get; set; }

	/// <summary>
	/// Cotação de compra da moeda consultada contra a unidade monetária corrente: unidade monetária corrente / [moeda].
	/// </summary>
	public decimal CotacaoCompra { get; set; }
	/// <summary>
	/// Cotação de venda da moeda consultada contra a unidade monetária corrente: unidade monetária corrente/[moeda].
	/// </summary>
	public decimal CotacaoVenda { get; set; }
	/// <summary>
	/// Data e hora da cotação Data, hora e minuto das paridades e cotações.
	/// </summary>
	public DateTime DataHoraCotacao { get; set; }
	/// <summary>
	/// Tipo do boletim Tipo das paridades e cotações para aquela data e hora.Podem ser dos tipos: Abertura, Intermediário, Fechamento Interbancário ou Fechamento.
	/// </summary>
	public string TipoBoletim { get; set; }
}


