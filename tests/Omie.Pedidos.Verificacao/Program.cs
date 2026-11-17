// Verificação do cliente do Omie, sem rede e sem biblioteca de teste: cada caso monta um OmieClient com um
// HttpMessageHandler falso, confere o JSON que seria enviado e simula a resposta da API.
// "dotnet run --project tests/Omie.Pedidos.Verificacao" sai com código 0 se todos os casos passarem.
using System.Net;
using ArrexTec.Omie.Pedidos;
using ArrexTec.Omie.Pedidos.Modelos;
using Omie.Pedidos.Verificacao;

// credencial falsa: a verificação nunca fala com o Omie de verdade
const string Chave = "chave-falsa";
const string Segredo = "segredo-falso";

static (OmieClient Cliente, HandlerFalso Handler) Montar(int maximoTentativas = 3)
{
    var handler = new HandlerFalso();
    var opcoes = new OmieOptions
    {
        AppKey = "  " + Chave + " ",       // espaços nas pontas: o envio tira, como o laboratório
        AppSecret = Segredo + "\n",
        MaximoTentativas = maximoTentativas,
        EsperaPorTentativa = TimeSpan.Zero, // sem espera real na verificação
    };
    return (new OmieClient(new HttpClient(handler), opcoes), handler);
}

static void ConferirEnvelope(RequisicaoCapturada r, string modulo, string call)
{
    Conferir.Igual(HttpMethod.Post, r.Metodo, "método");
    Conferir.Igual($"https://app.omie.com.br/api/v1/{modulo}/", r.Endereco.ToString(), "endereço");
    Conferir.Igual("application/json", r.TipoConteudo, "Content-Type");
    Conferir.Texto(call, r.Json["call"], "call");
    Conferir.Texto(Chave, r.Json["app_key"], "app_key sem espaços");
    Conferir.Texto(Segredo, r.Json["app_secret"], "app_secret sem espaços");
}

var casos = new List<(string Nome, Func<Task> Executar)>
{
    ("IncluirPedido: envia o pedido no formato do Omie e lê número e código", async () =>
    {
        var (cliente, handler) = Montar();
        // respostas simuladas: só os campos que o laboratório leu; descricao_status entra para provar OutrosCampos
        handler.ResponderOk("""
            { "codigo_pedido": 1234567890, "descricao_status": "Pedido cadastrado com sucesso!", "numero_pedido": "000000000000004" }
            """);

        var resposta = await cliente.IncluirPedidoAsync(new IncluirPedidoParametros
        {
            Cabecalho = new PedidoCabecalho
            {
                CodigoCliente = 9001,
                CodigoPedidoIntegracao = "PED-0001",
                DataPrevisao = new DateOnly(2026, 11, 20),
                Etapa = "10",
                CodigoParcela = "000",
            },
            Itens =
            [
                new PedidoItem
                {
                    Ide = new PedidoItemIde { CodigoItemIntegracao = "1" },
                    Produto = new PedidoItemProduto
                    {
                        CodigoProduto = 7001, Quantidade = 12, ValorUnitario = 8.9m, TipoDesconto = "P", PercentualDesconto = 3,
                    },
                },
                new PedidoItem
                {
                    Ide = new PedidoItemIde { CodigoItemIntegracao = "2" },
                    Produto = new PedidoItemProduto { CodigoProduto = 7002, Quantidade = 2.5m, ValorUnitario = 41.35m },
                },
            ],
            InformacoesAdicionais = new PedidoInformacoesAdicionais { CodigoCategoria = "1.01.01", CodigoContaCorrente = 555 },
            Observacoes = new PedidoObservacoes { ObsVenda = "Pedido recebido por e-mail, conferido e lançado." },
        });

        Conferir.Igual(1, handler.Requisicoes.Count, "uma chamada");
        var r = handler.Requisicoes[0];
        ConferirEnvelope(r, "produtos/pedido", "IncluirPedido");

        var cab = r.Param["cabecalho"]!.AsObject();
        Conferir.Numero(9001, cab["codigo_cliente"], "codigo_cliente");
        Conferir.Texto("PED-0001", cab["codigo_pedido_integracao"], "codigo_pedido_integracao");
        Conferir.Texto("20/11/2026", cab["data_previsao"], "data_previsao em dd/MM/aaaa");
        Conferir.Texto("10", cab["etapa"], "etapa");
        Conferir.Texto("000", cab["codigo_parcela"], "codigo_parcela");
        Conferir.Numero(2, cab["quantidade_itens"], "quantidade_itens preenchida pelo número de itens");

        var det = r.Param["det"]!.AsArray();
        Conferir.Igual(2, det.Count, "itens em det");
        Conferir.Texto("1", det[0]!["ide"]!["codigo_item_integracao"], "det[0].ide.codigo_item_integracao");
        var p0 = det[0]!["produto"]!.AsObject();
        Conferir.Numero(7001, p0["codigo_produto"], "det[0].produto.codigo_produto");
        Conferir.Numero(12, p0["quantidade"], "det[0].produto.quantidade");
        Conferir.Numero(8.9m, p0["valor_unitario"], "det[0].produto.valor_unitario");
        Conferir.Texto("P", p0["tipo_desconto"], "det[0].produto.tipo_desconto");
        Conferir.Numero(3, p0["percentual_desconto"], "det[0].produto.percentual_desconto");
        var p1 = det[1]!["produto"]!.AsObject();
        Conferir.Numero(2.5m, p1["quantidade"], "det[1].produto.quantidade decimal");
        Conferir.Ausente(p1, "tipo_desconto");
        Conferir.Ausente(p1, "percentual_desconto");

        var info = r.Param["informacoes_adicionais"]!.AsObject();
        Conferir.Texto("1.01.01", info["codigo_categoria"], "codigo_categoria");
        Conferir.Numero(555, info["codigo_conta_corrente"], "codigo_conta_corrente");
        Conferir.Texto("N", info["consumidor_final"], "consumidor_final como \"N\"");
        Conferir.Texto("N", info["enviar_email"], "enviar_email como \"N\"");
        Conferir.Texto("Pedido recebido por e-mail, conferido e lançado.", r.Param["observacoes"]!["obs_venda"], "obs_venda com acento");
        Conferir.Verdadeiro(r.Corpo.Contains("lançado"), "acento enviado como está, sem \\uXXXX");

        Conferir.Igual(1234567890L, resposta.CodigoPedido, "codigo_pedido");
        Conferir.Igual("000000000000004", resposta.NumeroPedido, "numero_pedido");
        Conferir.Verdadeiro(resposta.OutrosCampos?.ContainsKey("descricao_status") == true, "campo não tipado fica em OutrosCampos");
    }),

    ("IncluirPedido: numero_pedido aceito também como número", async () =>
    {
        var (cliente, handler) = Montar();
        handler.ResponderOk("""{ "codigo_pedido": "42", "numero_pedido": 17 }""");
        var resposta = await cliente.IncluirPedidoAsync(new IncluirPedidoParametros
        {
            Cabecalho = new PedidoCabecalho { CodigoCliente = 1, CodigoPedidoIntegracao = "X", DataPrevisao = new DateOnly(2026, 1, 2), QuantidadeItens = 1 },
            Itens = [new PedidoItem { Ide = new PedidoItemIde { CodigoItemIntegracao = "1" }, Produto = new PedidoItemProduto { CodigoProduto = 1, Quantidade = 1, ValorUnitario = 1 } }],
        });
        Conferir.Igual("17", resposta.NumeroPedido, "numero_pedido numérico");
        Conferir.Igual(42L, resposta.CodigoPedido, "codigo_pedido como texto");
        Conferir.Texto("02/01/2026", handler.Requisicoes[0].Param["cabecalho"]!["data_previsao"], "data com dois dígitos");
        Conferir.Ausente(handler.Requisicoes[0].Param, "informacoes_adicionais");
    }),

    ("ConsultarCliente: envia o código e corrige a acentuação trocada da resposta", async () =>
    {
        var (cliente, handler) = Montar();
        // "Laboratório" e "Observação" chegam como UTF-8 lido em Latin-1; "SÃO PAULO" já vem certo e fica como está
        handler.ResponderOk("""
            { "codigo_cliente_omie": 9001, "codigo_cliente_integracao": "CLI-01", "razao_social": "Cliente Exemplo Ltda",
              "cnpj_cpf": "00.000.000/0001-91", "cidade": "SÃO PAULO (SP)",
              "observacao": "LaboratÃ³rio de pedido. ObservaÃ§Ã£o: tabela B." }
            """);

        var c = await cliente.ConsultarClienteAsync(9001);

        var r = handler.Requisicoes.Single();
        ConferirEnvelope(r, "geral/clientes", "ConsultarCliente");
        Conferir.Numero(9001, r.Param["codigo_cliente_omie"], "codigo_cliente_omie");
        Conferir.Igual(1, r.Param.Count, "só codigo_cliente_omie no param");

        Conferir.Igual(9001L, c.CodigoClienteOmie, "codigo_cliente_omie");
        Conferir.Igual("CLI-01", c.CodigoClienteIntegracao, "codigo_cliente_integracao");
        Conferir.Igual("00.000.000/0001-91", c.CnpjCpf, "cnpj_cpf");
        Conferir.Igual("Laboratório de pedido. Observação: tabela B.", c.Observacao, "observacao corrigida");
        Conferir.Igual("SÃO PAULO (SP)", c.OutrosCampos!["cidade"].GetString(), "texto certo não é estragado");
    }),

    ("Erro da API: faultstring e faultcode viram OmieException, sem nova tentativa", async () =>
    {
        var (cliente, handler) = Montar();
        handler.Responder(HttpStatusCode.InternalServerError, """
            { "faultstring": "ERROR: Cliente não cadastrado para o Código [9999] !", "faultcode": "SOAP-ENV:Client-103" }
            """);

        var ex = await Conferir.LancaAsync<OmieException>(() => cliente.ConsultarClienteAsync(9999), "consulta recusada");

        Conferir.Igual(1, handler.Requisicoes.Count, "erro de negócio não é repetido");
        Conferir.Igual(OmieErroTipo.Api, ex.Tipo, "tipo");
        Conferir.Igual("ERROR: Cliente não cadastrado para o Código [9999] !", ex.FaultString, "faultstring");
        Conferir.Igual("SOAP-ENV:Client-103", ex.FaultCode, "faultcode");
        Conferir.Igual(HttpStatusCode.InternalServerError, ex.StatusHttp, "HTTP 500");
        Conferir.Igual("geral/clientes", ex.Modulo, "módulo");
        Conferir.Igual("ConsultarCliente", ex.Call, "call");
        Conferir.Verdadeiro(!ex.Message.Contains(Chave) && !ex.Message.Contains(Segredo), "credencial fora da mensagem");
    }),

    ("Erro da API: IncluirPedido recusado traz a faultstring", async () =>
    {
        var (cliente, handler) = Montar();
        handler.Responder(HttpStatusCode.InternalServerError, """{ "faultstring": "ERROR: Tag [codigo_produto] não informada!", "faultcode": "SOAP-ENV:Client-8" }""");
        var ex = await Conferir.LancaAsync<OmieException>(() => cliente.IncluirPedidoAsync(new IncluirPedidoParametros
        {
            Cabecalho = new PedidoCabecalho { CodigoCliente = 1, CodigoPedidoIntegracao = "X", DataPrevisao = new DateOnly(2026, 11, 20) },
            Itens = [],
        }), "pedido recusado");
        Conferir.Igual(OmieErroTipo.Api, ex.Tipo, "tipo");
        Conferir.Verdadeiro(ex.Message.Contains("Tag [codigo_produto]"), "mensagem com a faultstring");
    }),

    ("Bloqueio por consumo indevido: para na primeira, sem repetir", async () =>
    {
        var (cliente, handler) = Montar();
        handler.Responder(HttpStatusCode.InternalServerError, """{ "faultstring": "API bloqueada por consumo indevido. Tente novamente em 1500 segundos.", "faultcode": "SOAP-ENV:Client-6" }""");
        var ex = await Conferir.LancaAsync<OmieException>(() => cliente.ListarClientesAsync(), "bloqueio");
        Conferir.Igual(OmieErroTipo.Bloqueio, ex.Tipo, "tipo");
        Conferir.Igual(1, handler.Requisicoes.Count, "bloqueio não é repetido");
    }),

    ("Consumo redundante: não repete", async () =>
    {
        var (cliente, handler) = Montar();
        handler.Responder(HttpStatusCode.InternalServerError, """{ "faultstring": "Consumo redundante detectado.", "faultcode": "SOAP-ENV:Client-5" }""");
        var ex = await Conferir.LancaAsync<OmieException>(() => cliente.ListarProdutosResumidoAsync(), "redundante");
        Conferir.Igual(OmieErroTipo.ConsumoRedundante, ex.Tipo, "tipo");
        Conferir.Igual(1, handler.Requisicoes.Count, "redundante não é repetido");
    }),

    ("Limite de frequência: espera e tenta de novo, e passa na segunda", async () =>
    {
        var (cliente, handler) = Montar();
        handler.Responder(HttpStatusCode.InternalServerError, """{ "faultstring": "Limite de requisições atingido. Aguarde.", "faultcode": "SOAP-ENV:Client-4" }""");
        handler.ResponderOk("""{ "produto_servico_resumido": [ { "codigo": "DET-500" }, { "codigo": "ALV-1L" } ] }""");

        var resposta = await cliente.ListarProdutosResumidoAsync();

        Conferir.Igual(2, handler.Requisicoes.Count, "duas chamadas");
        Conferir.Igual(handler.Requisicoes[0].Corpo, handler.Requisicoes[1].Corpo, "mesmo corpo na nova tentativa");
        Conferir.Igual(2, resposta.Produtos!.Count, "produtos lidos");
        Conferir.Igual("DET-500", resposta.Produtos[0].Codigo, "codigo");
    }),

    ("Limite de frequência: desiste depois de MaximoTentativas", async () =>
    {
        var (cliente, handler) = Montar(maximoTentativas: 3);
        for (var i = 0; i < 3; i++)
        {
            handler.Responder(HttpStatusCode.InternalServerError, """{ "faultstring": "Aguarde 2 segundos.", "faultcode": "SOAP-ENV:Client-4" }""");
        }

        var ex = await Conferir.LancaAsync<OmieException>(() => cliente.ListarProdutosResumidoAsync(), "limite esgotado");
        Conferir.Igual(OmieErroTipo.LimiteFrequencia, ex.Tipo, "tipo");
        Conferir.Igual(3, handler.Requisicoes.Count, "três tentativas no total");
    }),

    ("UpsertProduto e UpsertCliente: campos e sim/não no formato do Omie", async () =>
    {
        var (cliente, handler) = Montar();
        handler.ResponderOk("""{ "codigo_produto": 7001, "descricao_status": "Produto cadastrado com sucesso!" }""");
        handler.ResponderOk("""{ "codigo_cliente_omie": 9001, "descricao_status": "Cliente cadastrado com sucesso!" }""");

        var rp = await cliente.UpsertProdutoAsync(new UpsertProdutoParametros
        {
            CodigoProdutoIntegracao = "DET-500", Codigo = "DET-500", Descricao = "Detergente neutro 500 ml (cx 24)",
            Unidade = "CX", Ncm = "3402.20.00", ValorUnitario = 52.8m, ObsInternas = "Exemplo.",
        });
        var rc = await cliente.UpsertClienteAsync(new UpsertClienteParametros
        {
            CodigoClienteIntegracao = "CLI-01", RazaoSocial = "Cliente Exemplo Ltda", NomeFantasia = "Cliente Exemplo",
            CnpjCpf = "00.000.000/0001-91", Cidade = "CAMPINAS (SP)", Estado = "SP", Observacao = "Tabela B.",
            Tags = [new ClienteTag("exemplo"), new ClienteTag("tabela-B")],
        });

        var p = handler.Requisicoes[0];
        ConferirEnvelope(p, "geral/produtos", "UpsertProduto");
        Conferir.Texto("DET-500", p.Param["codigo_produto_integracao"], "codigo_produto_integracao");
        Conferir.Texto("CX", p.Param["unidade"], "unidade");
        Conferir.Texto("3402.20.00", p.Param["ncm"], "ncm");
        Conferir.Numero(52.8m, p.Param["valor_unitario"], "valor_unitario");
        Conferir.Igual("Produto cadastrado com sucesso!", rp.DescricaoStatus, "descricao_status do produto");

        var c = handler.Requisicoes[1];
        ConferirEnvelope(c, "geral/clientes", "UpsertCliente");
        Conferir.Texto("N", c.Param["pessoa_fisica"], "pessoa_fisica como \"N\"");
        Conferir.Texto("SP", c.Param["estado"], "estado");
        Conferir.Texto("tabela-B", c.Param["tags"]![1]!["tag"], "tags[1].tag");
        Conferir.Igual("Cliente cadastrado com sucesso!", rc.DescricaoStatus, "descricao_status do cliente");
    }),

    ("Listagens: parâmetros padrão e leitura de clientes, produtos e contas correntes", async () =>
    {
        var (cliente, handler) = Montar();
        handler.ResponderOk("""{ "pagina": 1, "clientes_cadastro": [ { "codigo_cliente_omie": "9001", "codigo_cliente_integracao": "CLI-01", "razao_social": "Cliente Exemplo Ltda", "cnpj_cpf": "00.000.000/0001-91" } ] }""");
        handler.ResponderOk("""{ "produto_servico_cadastro": [ { "codigo_produto": 7001, "codigo_produto_integracao": "DET-500", "codigo": "DET-500", "descricao": "Detergente neutro 500 ml (cx 24)", "unidade": "CX", "valor_unitario": 52.8 } ] }""");
        handler.ResponderOk("""{ "ListarContasCorrentes": [ { "nCodCC": 555 } ] }""");
        handler.ResponderOk("""{ "conta_corrente_lista": [ { "codigo_conta_corrente": 777 } ] }""");

        var clientes = await cliente.ListarClientesAsync();
        var produtos = await cliente.ListarProdutosAsync();
        var contas1 = await cliente.ListarContasCorrentesAsync();
        var contas2 = await cliente.ListarContasCorrentesAsync();

        var lc = handler.Requisicoes[0];
        ConferirEnvelope(lc, "geral/clientes", "ListarClientes");
        Conferir.Numero(1, lc.Param["pagina"], "pagina");
        Conferir.Numero(100, lc.Param["registros_por_pagina"], "registros_por_pagina");
        Conferir.Texto("N", lc.Param["apenas_importado_api"], "apenas_importado_api");
        Conferir.Ausente(lc.Param, "filtrar_apenas_omiepdv");

        var lp = handler.Requisicoes[1];
        ConferirEnvelope(lp, "geral/produtos", "ListarProdutos");
        Conferir.Texto("N", lp.Param["filtrar_apenas_omiepdv"], "filtrar_apenas_omiepdv");

        var lcc = handler.Requisicoes[2];
        ConferirEnvelope(lcc, "geral/contacorrente", "ListarContasCorrentes");
        Conferir.Numero(50, lcc.Param["registros_por_pagina"], "registros_por_pagina das contas");

        Conferir.Igual(9001L, clientes.Clientes![0].CodigoClienteOmie, "codigo_cliente_omie vindo como texto");
        Conferir.Igual(7001L, produtos.Produtos![0].CodigoProduto, "codigo_produto");
        Conferir.Igual(52.8m, produtos.Produtos[0].ValorUnitario, "valor_unitario");
        Conferir.Igual(555L, contas1.Contas[0].Codigo, "conta em ListarContasCorrentes/nCodCC");
        Conferir.Igual(777L, contas2.Contas[0].Codigo, "conta em conta_corrente_lista/codigo_conta_corrente");
    }),

    ("Resposta que não é JSON vira OmieException de resposta inválida", async () =>
    {
        var (cliente, handler) = Montar();
        handler.Responder(HttpStatusCode.BadGateway, "<html>502</html>");
        var ex = await Conferir.LancaAsync<OmieException>(() => cliente.ListarClientesResumidoAsync(), "502 em HTML");
        Conferir.Igual(OmieErroTipo.RespostaInvalida, ex.Tipo, "tipo");
        Conferir.Igual(HttpStatusCode.BadGateway, ex.StatusHttp, "HTTP 502");
    }),

    ("Construtor recusa credencial vazia", () =>
    {
        Conferir.Verdadeiro(Lanca(() => new OmieClient(new HttpClient(), new OmieOptions { AppKey = "", AppSecret = "x" })), "AppKey vazia");
        Conferir.Verdadeiro(Lanca(() => new OmieClient(new HttpClient(), new OmieOptions { AppKey = "x", AppSecret = " " })), "AppSecret vazio");
        return Task.CompletedTask;

        static bool Lanca(Action acao)
        {
            try { acao(); return false; } catch (ArgumentException) { return true; }
        }
    }),
};

var falhas = 0;
foreach (var (nome, executar) in casos)
{
    try
    {
        await executar();
        Console.WriteLine($"ok     {nome}");
    }
    catch (Exception ex)
    {
        falhas++;
        Console.WriteLine($"FALHOU {nome}\n       {ex.GetType().Name}: {ex.Message}");
    }
}

Console.WriteLine();
Console.WriteLine(falhas == 0 ? $"{casos.Count} de {casos.Count} casos passaram." : $"{falhas} de {casos.Count} casos falharam.");
return falhas == 0 ? 0 : 1;
