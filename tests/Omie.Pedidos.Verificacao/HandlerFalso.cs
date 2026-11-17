using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace Omie.Pedidos.Verificacao;

/// <summary>Uma requisição que o cliente enviou, guardada para conferência.</summary>
internal sealed record RequisicaoCapturada(HttpMethod Metodo, Uri Endereco, string? TipoConteudo, string Corpo)
{
    /// <summary>O corpo como JSON, para conferir campo a campo.</summary>
    public JsonObject Json => JsonNode.Parse(Corpo)?.AsObject() ?? throw new FalhaVerificacao("corpo vazio");

    /// <summary>O único elemento de <c>param</c>.</summary>
    public JsonObject Param
    {
        get
        {
            var lista = Json["param"]?.AsArray() ?? throw new FalhaVerificacao("sem param");
            Conferir.Igual(1, lista.Count, "param tem um único elemento");
            return lista[0]?.AsObject() ?? throw new FalhaVerificacao("param[0] vazio");
        }
    }
}

/// <summary>
/// HttpMessageHandler falso: não acessa a rede, guarda cada requisição e devolve as respostas da fila, na ordem.
/// </summary>
internal sealed class HandlerFalso : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode Status, string Corpo)> _respostas = new();

    public List<RequisicaoCapturada> Requisicoes { get; } = [];

    /// <summary>Enfileira a próxima resposta.</summary>
    public HandlerFalso Responder(HttpStatusCode status, string corpo)
    {
        _respostas.Enqueue((status, corpo));
        return this;
    }

    /// <summary>Enfileira uma resposta 200 com o JSON informado.</summary>
    public HandlerFalso ResponderOk(string json) => Responder(HttpStatusCode.OK, json);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var corpo = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
        Requisicoes.Add(new RequisicaoCapturada(request.Method, request.RequestUri!, request.Content?.Headers.ContentType?.MediaType, corpo));

        if (_respostas.Count == 0)
        {
            throw new FalhaVerificacao($"chamada a mais, sem resposta preparada: {corpo}");
        }

        var (status, json) = _respostas.Dequeue();
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }
}
