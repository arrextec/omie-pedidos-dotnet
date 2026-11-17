using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ArrexTec.Omie.Pedidos.Json;
using ArrexTec.Omie.Pedidos.Modelos;

namespace ArrexTec.Omie.Pedidos;

/// <summary>Caminhos dos módulos da API do Omie usados por este pacote.</summary>
public static class OmieModulos
{
    /// <summary>Cadastro de clientes.</summary>
    public const string Clientes = "geral/clientes";

    /// <summary>Cadastro de produtos.</summary>
    public const string Produtos = "geral/produtos";

    /// <summary>Contas correntes.</summary>
    public const string ContasCorrentes = "geral/contacorrente";

    /// <summary>Pedido de venda.</summary>
    public const string PedidoVenda = "produtos/pedido";
}

/// <summary>
/// Cliente tipado da API do Omie para lançar pedidos de venda. Cada chamada é um POST JSON em
/// <c>{EnderecoBase}{modulo}/</c> com <c>{ call, app_key, app_secret, param: [ ... ] }</c>.
/// </summary>
/// <remarks>
/// O <see cref="HttpClient"/> é seu: crie com <c>IHttpClientFactory</c> ou reaproveite uma instância; este
/// cliente não o descarta. A classe não guarda estado entre chamadas e pode ser usada por várias threads.
/// </remarks>
public sealed class OmieClient
{
    private readonly HttpClient _http;
    private readonly OmieOptions _opcoes;

    /// <summary>Cria o cliente.</summary>
    /// <param name="httpClient">HttpClient usado nas chamadas (injetável; em testes, com um handler falso).</param>
    /// <param name="opcoes">Credencial e comportamento do cliente.</param>
    /// <exception cref="ArgumentException">Sem <c>AppKey</c> ou <c>AppSecret</c>.</exception>
    public OmieClient(HttpClient httpClient, OmieOptions opcoes)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(opcoes);
        if (string.IsNullOrWhiteSpace(opcoes.AppKey) || string.IsNullOrWhiteSpace(opcoes.AppSecret))
        {
            throw new ArgumentException("Informe AppKey e AppSecret do aplicativo do Omie.", nameof(opcoes));
        }

        if (opcoes.MaximoTentativas < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(opcoes), "MaximoTentativas precisa ser 1 ou mais.");
        }

        _http = httpClient;
        _opcoes = opcoes;
    }

    // ------------------------------------------------------------------------------------------------ clientes

    /// <summary><c>ListarClientesResumido</c>: uma página de clientes, só com os campos resumidos.</summary>
    public Task<ListarClientesResumidoResposta> ListarClientesResumidoAsync(
        ListarClientesParametros? parametros = null, CancellationToken cancellationToken = default) =>
        ChamarAsync<ListarClientesResumidoResposta>(
            OmieModulos.Clientes, "ListarClientesResumido", parametros ?? new ListarClientesParametros(), cancellationToken);

    /// <summary><c>ListarClientes</c>: uma página de clientes com o cadastro (sem a observação).</summary>
    public Task<ListarClientesResposta> ListarClientesAsync(
        ListarClientesParametros? parametros = null, CancellationToken cancellationToken = default) =>
        ChamarAsync<ListarClientesResposta>(
            OmieModulos.Clientes, "ListarClientes", parametros ?? new ListarClientesParametros(), cancellationToken);

    /// <summary><c>ConsultarCliente</c>: o cadastro completo de um cliente, inclusive a observação.</summary>
    /// <param name="codigoClienteOmie">Código do cliente no Omie (<c>codigo_cliente_omie</c>).</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    public Task<ClienteCadastro> ConsultarClienteAsync(long codigoClienteOmie, CancellationToken cancellationToken = default) =>
        ChamarAsync<ClienteCadastro>(
            OmieModulos.Clientes,
            "ConsultarCliente",
            new ConsultarClienteParametros { CodigoClienteOmie = codigoClienteOmie },
            cancellationToken);

    /// <summary><c>UpsertCliente</c>: inclui ou altera o cliente pelo código de integração.</summary>
    public Task<UpsertResposta> UpsertClienteAsync(UpsertClienteParametros cliente, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        return ChamarAsync<UpsertResposta>(OmieModulos.Clientes, "UpsertCliente", cliente, cancellationToken);
    }

    // ------------------------------------------------------------------------------------------------ produtos

    /// <summary><c>ListarProdutosResumido</c>: uma página de produtos, só com os campos resumidos.</summary>
    public Task<ListarProdutosResumidoResposta> ListarProdutosResumidoAsync(
        ListarProdutosParametros? parametros = null, CancellationToken cancellationToken = default) =>
        ChamarAsync<ListarProdutosResumidoResposta>(
            OmieModulos.Produtos, "ListarProdutosResumido", parametros ?? new ListarProdutosParametros(), cancellationToken);

    /// <summary><c>ListarProdutos</c>: uma página de produtos com o cadastro (código, descrição, unidade e preço).</summary>
    public Task<ListarProdutosResposta> ListarProdutosAsync(
        ListarProdutosParametros? parametros = null, CancellationToken cancellationToken = default) =>
        ChamarAsync<ListarProdutosResposta>(
            OmieModulos.Produtos, "ListarProdutos", parametros ?? new ListarProdutosParametros(), cancellationToken);

    /// <summary><c>UpsertProduto</c>: inclui ou altera o produto pelo código de integração.</summary>
    public Task<UpsertResposta> UpsertProdutoAsync(UpsertProdutoParametros produto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(produto);
        return ChamarAsync<UpsertResposta>(OmieModulos.Produtos, "UpsertProduto", produto, cancellationToken);
    }

    // ---------------------------------------------------------------------------------------- contas correntes

    /// <summary><c>ListarContasCorrentes</c>: as contas correntes, para escolher a que vai no pedido.</summary>
    public Task<ListarContasCorrentesResposta> ListarContasCorrentesAsync(
        ListarContasCorrentesParametros? parametros = null, CancellationToken cancellationToken = default) =>
        ChamarAsync<ListarContasCorrentesResposta>(
            OmieModulos.ContasCorrentes, "ListarContasCorrentes", parametros ?? new ListarContasCorrentesParametros(), cancellationToken);

    // ---------------------------------------------------------------------------------------- pedido de venda

    /// <summary>
    /// <c>IncluirPedido</c>: grava o pedido de venda. Se <see cref="PedidoCabecalho.QuantidadeItens"/> estiver vazio,
    /// é preenchido com o número de itens.
    /// </summary>
    public Task<IncluirPedidoResposta> IncluirPedidoAsync(IncluirPedidoParametros pedido, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        pedido.Cabecalho.QuantidadeItens ??= pedido.Itens.Count;
        return ChamarAsync<IncluirPedidoResposta>(OmieModulos.PedidoVenda, "IncluirPedido", pedido, cancellationToken);
    }

    // ------------------------------------------------------------------------------------------- chamada genérica

    /// <summary>
    /// Faz qualquer chamada da API do Omie e lê a resposta em <typeparamref name="TResposta"/>. Use para o que
    /// este pacote ainda não tipou.
    /// </summary>
    /// <typeparam name="TResposta">Tipo da resposta (pode ser <see cref="JsonNode"/> ou <see cref="JsonElement"/>).</typeparam>
    /// <param name="modulo">Caminho do módulo, sem barras nas pontas (ex.: <c>produtos/pedido</c>).</param>
    /// <param name="call">Nome da chamada (ex.: <c>IncluirPedido</c>).</param>
    /// <param name="parametros">Objeto que vai como único elemento de <c>param</c>.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <exception cref="OmieException">O Omie recusou a chamada ou a resposta não pôde ser lida.</exception>
    public async Task<TResposta> ChamarAsync<TResposta>(
        string modulo, string call, object parametros, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modulo);
        ArgumentException.ThrowIfNullOrWhiteSpace(call);
        ArgumentNullException.ThrowIfNull(parametros);

        var endereco = new Uri(_opcoes.EnderecoBase, modulo.Trim('/') + "/");
        var corpo = MontarCorpo(call, parametros);

        for (var tentativa = 1; ; tentativa++)
        {
            using var requisicao = new HttpRequestMessage(HttpMethod.Post, endereco)
            {
                Content = new StringContent(corpo, Encoding.UTF8, "application/json"),
            };

            using var resposta = await _http.SendAsync(requisicao, cancellationToken).ConfigureAwait(false);
            var texto = await resposta.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var json = LerJson(texto, resposta.StatusCode, modulo, call);

            if (_opcoes.CorrigirAcentuacao)
            {
                OmieJson.CorrigirAcentuacao(json);
            }

            // a API recusa com { faultstring, faultcode }, em geral com HTTP 500
            if (json is JsonObject objeto && objeto["faultstring"] is { } noFalha)
            {
                var faultString = TextoDe(noFalha);
                var faultCode = objeto["faultcode"] is { } noCodigo ? TextoDe(noCodigo) : null;
                var tipo = Classificar(faultString);

                if (tipo == OmieErroTipo.LimiteFrequencia && tentativa < _opcoes.MaximoTentativas)
                {
                    await Task.Delay(_opcoes.EsperaPorTentativa * tentativa, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                throw new OmieException(
                    $"O Omie recusou {call} ({modulo}): {faultString}",
                    modulo, call, tipo, faultString, faultCode, resposta.StatusCode);
            }

            if (!resposta.IsSuccessStatusCode)
            {
                throw new OmieException(
                    $"O Omie respondeu HTTP {(int)resposta.StatusCode} em {call} ({modulo}) sem faultstring.",
                    modulo, call, OmieErroTipo.RespostaInvalida, statusHttp: resposta.StatusCode);
            }

            try
            {
                return json.Deserialize<TResposta>(OmieJson.Opcoes)
                    ?? throw new JsonException("Resposta vazia.");
            }
            catch (JsonException ex)
            {
                throw new OmieException(
                    $"Não foi possível ler a resposta de {call} ({modulo}): {ex.Message}",
                    modulo, call, OmieErroTipo.RespostaInvalida, statusHttp: resposta.StatusCode, interna: ex);
            }
        }
    }

    /// <summary>Monta o envelope da chamada. A credencial vai sem espaços nas pontas e nunca entra em mensagem de erro.</summary>
    private string MontarCorpo(string call, object parametros)
    {
        var envelope = new JsonObject
        {
            ["call"] = call,
            ["app_key"] = _opcoes.AppKey.Trim(),
            ["app_secret"] = _opcoes.AppSecret.Trim(),
            ["param"] = new JsonArray(JsonSerializer.SerializeToNode(parametros, parametros.GetType(), OmieJson.Opcoes)),
        };
        return envelope.ToJsonString(OmieJson.Opcoes);
    }

    private static JsonNode? LerJson(string texto, HttpStatusCode status, string modulo, string call)
    {
        try
        {
            return JsonNode.Parse(texto);
        }
        catch (JsonException ex)
        {
            throw new OmieException(
                $"O Omie respondeu HTTP {(int)status} em {call} ({modulo}) com conteúdo que não é JSON.",
                modulo, call, OmieErroTipo.RespostaInvalida, statusHttp: status, interna: ex);
        }
    }

    private static string TextoDe(JsonNode no) =>
        no is JsonValue valor && valor.TryGetValue<string>(out var texto) ? texto : no.ToJsonString();

    /// <summary>
    /// Classifica a recusa pelo texto da <c>faultstring</c>, como o laboratório faz: bloqueio primeiro (nunca repetir),
    /// depois consumo redundante (repetir não adianta), depois limite de frequência (repetir depois de esperar).
    /// </summary>
    internal static OmieErroTipo Classificar(string faultString)
    {
        if (Contem(faultString, "bloqueada") || Contem(faultString, "consumo indevido"))
        {
            return OmieErroTipo.Bloqueio;
        }

        if (Contem(faultString, "redundante"))
        {
            return OmieErroTipo.ConsumoRedundante;
        }

        if (Contem(faultString, "aguarde") || Contem(faultString, "limite"))
        {
            return OmieErroTipo.LimiteFrequencia;
        }

        return OmieErroTipo.Api;
    }

    private static bool Contem(string texto, string trecho) =>
        texto.Contains(trecho, StringComparison.OrdinalIgnoreCase);
}
