using System.Text.Json.Serialization;
using ArrexTec.Omie.Pedidos.Json;

namespace ArrexTec.Omie.Pedidos.Modelos;

/// <summary>Parâmetros de <c>IncluirPedido</c> (módulo <c>produtos/pedido</c>): um pedido de venda.</summary>
public sealed class IncluirPedidoParametros
{
    /// <summary>Cabeçalho do pedido (<c>cabecalho</c>).</summary>
    [JsonPropertyName("cabecalho")]
    public required PedidoCabecalho Cabecalho { get; set; }

    /// <summary>Itens do pedido (<c>det</c>).</summary>
    [JsonPropertyName("det")]
    public required List<PedidoItem> Itens { get; set; }

    /// <summary>Informações adicionais (<c>informacoes_adicionais</c>): categoria, conta corrente etc.</summary>
    [JsonPropertyName("informacoes_adicionais")]
    public PedidoInformacoesAdicionais? InformacoesAdicionais { get; set; }

    /// <summary>Observações do pedido (<c>observacoes</c>).</summary>
    [JsonPropertyName("observacoes")]
    public PedidoObservacoes? Observacoes { get; set; }
}

/// <summary>Cabeçalho do pedido de venda.</summary>
public sealed class PedidoCabecalho
{
    /// <summary>Código do cliente no Omie (<c>codigo_cliente</c>), o <see cref="ClienteCadastro.CodigoClienteOmie"/>.</summary>
    [JsonPropertyName("codigo_cliente")]
    public required long CodigoCliente { get; set; }

    /// <summary>
    /// Código do pedido no seu sistema (<c>codigo_pedido_integracao</c>). Use um código único por pedido:
    /// é o que identifica o pedido entre o seu sistema e o Omie.
    /// </summary>
    [JsonPropertyName("codigo_pedido_integracao")]
    public required string CodigoPedidoIntegracao { get; set; }

    /// <summary>Data de previsão (<c>data_previsao</c>), enviada como dd/MM/aaaa.</summary>
    [JsonPropertyName("data_previsao")]
    [JsonConverter(typeof(DataOmieConverter))]
    public required DateOnly DataPrevisao { get; set; }

    /// <summary>Etapa do pedido no Omie (<c>etapa</c>). O laboratório usou "10".</summary>
    [JsonPropertyName("etapa")]
    public string? Etapa { get; set; }

    /// <summary>Código da condição de parcelamento (<c>codigo_parcela</c>). O laboratório usou "000".</summary>
    [JsonPropertyName("codigo_parcela")]
    public string? CodigoParcela { get; set; }

    /// <summary>
    /// Quantidade de itens (<c>quantidade_itens</c>). Se ficar vazio, <see cref="OmieClient.IncluirPedidoAsync"/>
    /// preenche com o número de itens do pedido.
    /// </summary>
    [JsonPropertyName("quantidade_itens")]
    public int? QuantidadeItens { get; set; }
}

/// <summary>Item do pedido de venda (um elemento de <c>det</c>).</summary>
public sealed class PedidoItem
{
    /// <summary>Identificação do item (<c>ide</c>).</summary>
    [JsonPropertyName("ide")]
    public required PedidoItemIde Ide { get; set; }

    /// <summary>Produto, quantidade, preço e desconto do item (<c>produto</c>).</summary>
    [JsonPropertyName("produto")]
    public required PedidoItemProduto Produto { get; set; }
}

/// <summary>Identificação do item no pedido.</summary>
public sealed class PedidoItemIde
{
    /// <summary>Código do item no seu sistema (<c>codigo_item_integracao</c>), ex.: "1", "2"…</summary>
    [JsonPropertyName("codigo_item_integracao")]
    public required string CodigoItemIntegracao { get; set; }
}

/// <summary>Produto do item do pedido.</summary>
public sealed class PedidoItemProduto
{
    /// <summary>Código do produto no Omie (<c>codigo_produto</c>), o <see cref="ProdutoCadastro.CodigoProduto"/>.</summary>
    [JsonPropertyName("codigo_produto")]
    public required long CodigoProduto { get; set; }

    /// <summary>Quantidade, na unidade de venda do cadastro (<c>quantidade</c>).</summary>
    [JsonPropertyName("quantidade")]
    public required decimal Quantidade { get; set; }

    /// <summary>Preço unitário (<c>valor_unitario</c>). O preço vem da sua tabela, não do que o comprador escreveu.</summary>
    [JsonPropertyName("valor_unitario")]
    public required decimal ValorUnitario { get; set; }

    /// <summary>Tipo de desconto (<c>tipo_desconto</c>). O laboratório usou "P" (percentual).</summary>
    [JsonPropertyName("tipo_desconto")]
    public string? TipoDesconto { get; set; }

    /// <summary>Percentual de desconto (<c>percentual_desconto</c>), quando <see cref="TipoDesconto"/> é "P".</summary>
    [JsonPropertyName("percentual_desconto")]
    public decimal? PercentualDesconto { get; set; }
}

/// <summary>Informações adicionais do pedido de venda.</summary>
public sealed class PedidoInformacoesAdicionais
{
    /// <summary>Categoria da receita (<c>codigo_categoria</c>). O laboratório usou "1.01.01".</summary>
    [JsonPropertyName("codigo_categoria")]
    public string? CodigoCategoria { get; set; }

    /// <summary>Conta corrente (<c>codigo_conta_corrente</c>), o <see cref="ContaCorrente.Codigo"/>.</summary>
    [JsonPropertyName("codigo_conta_corrente")]
    public long? CodigoContaCorrente { get; set; }

    /// <summary>Consumidor final (<c>consumidor_final</c>, "S"/"N"). O padrão é "N".</summary>
    [JsonPropertyName("consumidor_final")]
    [JsonConverter(typeof(SimNaoConverter))]
    public bool ConsumidorFinal { get; set; }

    /// <summary>O Omie envia o pedido por e-mail ao cliente (<c>enviar_email</c>, "S"/"N"). O padrão é "N".</summary>
    [JsonPropertyName("enviar_email")]
    [JsonConverter(typeof(SimNaoConverter))]
    public bool EnviarEmail { get; set; }
}

/// <summary>Observações do pedido de venda.</summary>
public sealed class PedidoObservacoes
{
    /// <summary>Observação da venda (<c>obs_venda</c>).</summary>
    [JsonPropertyName("obs_venda")]
    public string? ObsVenda { get; set; }
}

/// <summary>Resposta de <c>IncluirPedido</c>.</summary>
public sealed class IncluirPedidoResposta : OmieObjeto
{
    /// <summary>Código interno do pedido no Omie (<c>codigo_pedido</c>).</summary>
    [JsonPropertyName("codigo_pedido")]
    public long? CodigoPedido { get; set; }

    /// <summary>Número do pedido, como aparece na tela do Omie (<c>numero_pedido</c>).</summary>
    [JsonPropertyName("numero_pedido")]
    [JsonConverter(typeof(TextoOuNumeroConverter))]
    public string? NumeroPedido { get; set; }
}
