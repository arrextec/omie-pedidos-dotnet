using System.Net;

namespace ArrexTec.Omie.Pedidos;

/// <summary>Que tipo de recusa o Omie devolveu.</summary>
public enum OmieErroTipo
{
    /// <summary>Erro de negócio ou de validação devolvido pela API (campo obrigatório, registro inexistente etc.).</summary>
    Api,

    /// <summary>
    /// A API foi bloqueada por consumo indevido (chamadas repetidas). O bloqueio dura em torno de 25 minutos e
    /// cada nova chamada o estende: pare e tente mais tarde. Nunca é repetido automaticamente.
    /// </summary>
    Bloqueio,

    /// <summary>
    /// A mesma consulta foi repetida em menos de cerca de 1 minuto ("consumo redundante"). Use o resultado
    /// anterior em vez de chamar de novo. Não é repetido automaticamente.
    /// </summary>
    ConsumoRedundante,

    /// <summary>
    /// Limite de frequência ("aguarde"/"limite"). O cliente já tentou de novo até
    /// <see cref="OmieOptions.MaximoTentativas"/> vezes antes de lançar este erro.
    /// </summary>
    LimiteFrequencia,

    /// <summary>A resposta não veio em JSON, ou veio num formato que não pôde ser lido.</summary>
    RespostaInvalida,
}

/// <summary>
/// Erro devolvido pela API do Omie. Quando a API recusa uma chamada, ela responde com um JSON que traz
/// <c>faultstring</c> (a mensagem) e <c>faultcode</c> (o código), em geral com HTTP 500.
/// </summary>
public sealed class OmieException : Exception
{
    /// <summary>Cria o erro com os dados da chamada e da resposta.</summary>
    /// <param name="mensagem">Mensagem legível do erro.</param>
    /// <param name="modulo">Módulo chamado (ex.: <c>produtos/pedido</c>).</param>
    /// <param name="call">Nome da chamada (ex.: <c>IncluirPedido</c>).</param>
    /// <param name="tipo">Classificação do erro.</param>
    /// <param name="faultString">Valor de <c>faultstring</c> na resposta, se houver.</param>
    /// <param name="faultCode">Valor de <c>faultcode</c> na resposta, se houver.</param>
    /// <param name="statusHttp">Código HTTP da resposta, se houve resposta.</param>
    /// <param name="interna">Exceção de origem, se houver.</param>
    public OmieException(
        string mensagem,
        string modulo,
        string call,
        OmieErroTipo tipo,
        string? faultString = null,
        string? faultCode = null,
        HttpStatusCode? statusHttp = null,
        Exception? interna = null)
        : base(mensagem, interna)
    {
        Modulo = modulo;
        Call = call;
        Tipo = tipo;
        FaultString = faultString;
        FaultCode = faultCode;
        StatusHttp = statusHttp;
    }

    /// <summary>Módulo chamado (ex.: <c>produtos/pedido</c>).</summary>
    public string Modulo { get; }

    /// <summary>Nome da chamada (ex.: <c>IncluirPedido</c>).</summary>
    public string Call { get; }

    /// <summary>Classificação do erro, para decidir se vale tentar de novo.</summary>
    public OmieErroTipo Tipo { get; }

    /// <summary>Mensagem devolvida pela API em <c>faultstring</c>.</summary>
    public string? FaultString { get; }

    /// <summary>Código devolvido pela API em <c>faultcode</c>.</summary>
    public string? FaultCode { get; }

    /// <summary>Código HTTP da resposta.</summary>
    public HttpStatusCode? StatusHttp { get; }
}
