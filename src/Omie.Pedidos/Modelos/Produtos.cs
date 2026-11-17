using System.Text.Json.Serialization;
using ArrexTec.Omie.Pedidos.Json;

namespace ArrexTec.Omie.Pedidos.Modelos;

/// <summary>Parâmetros de <c>ListarProdutos</c> e <c>ListarProdutosResumido</c> (módulo <c>geral/produtos</c>).</summary>
public sealed class ListarProdutosParametros
{
    /// <summary>Página a ler, a partir de 1 (<c>pagina</c>).</summary>
    [JsonPropertyName("pagina")]
    public int Pagina { get; set; } = 1;

    /// <summary>Quantos registros por página (<c>registros_por_pagina</c>). O laboratório usou 100.</summary>
    [JsonPropertyName("registros_por_pagina")]
    public int RegistrosPorPagina { get; set; } = 100;

    /// <summary>Só os registros incluídos pela API (<c>apenas_importado_api</c>, "S"/"N").</summary>
    [JsonPropertyName("apenas_importado_api")]
    [JsonConverter(typeof(SimNaoConverter))]
    public bool ApenasImportadoApi { get; set; }

    /// <summary>Só os produtos do Omie PDV (<c>filtrar_apenas_omiepdv</c>, "S"/"N").</summary>
    [JsonPropertyName("filtrar_apenas_omiepdv")]
    [JsonConverter(typeof(SimNaoConverter))]
    public bool FiltrarApenasOmiePdv { get; set; }
}

/// <summary>Resposta de <c>ListarProdutosResumido</c>.</summary>
public sealed class ListarProdutosResumidoResposta : OmieObjeto
{
    /// <summary>Produtos da página (<c>produto_servico_resumido</c>).</summary>
    [JsonPropertyName("produto_servico_resumido")]
    public List<ProdutoResumido>? Produtos { get; set; }
}

/// <summary>Produto na listagem resumida.</summary>
public sealed class ProdutoResumido : OmieObjeto
{
    /// <summary>Código do produto (<c>codigo</c>), o SKU visível no Omie.</summary>
    [JsonPropertyName("codigo")]
    public string? Codigo { get; set; }
}

/// <summary>Resposta de <c>ListarProdutos</c>.</summary>
public sealed class ListarProdutosResposta : OmieObjeto
{
    /// <summary>Produtos da página (<c>produto_servico_cadastro</c>).</summary>
    [JsonPropertyName("produto_servico_cadastro")]
    public List<ProdutoCadastro>? Produtos { get; set; }
}

/// <summary>Produto como o Omie devolve em <c>ListarProdutos</c>.</summary>
public sealed class ProdutoCadastro : OmieObjeto
{
    /// <summary>Código do produto no Omie (<c>codigo_produto</c>). É o que vai no item do pedido.</summary>
    [JsonPropertyName("codigo_produto")]
    public long CodigoProduto { get; set; }

    /// <summary>Código do produto no seu sistema (<c>codigo_produto_integracao</c>).</summary>
    [JsonPropertyName("codigo_produto_integracao")]
    public string? CodigoProdutoIntegracao { get; set; }

    /// <summary>Código do produto (<c>codigo</c>), o SKU visível no Omie.</summary>
    [JsonPropertyName("codigo")]
    public string? Codigo { get; set; }

    /// <summary>Descrição (<c>descricao</c>).</summary>
    [JsonPropertyName("descricao")]
    public string? Descricao { get; set; }

    /// <summary>Sigla da unidade (<c>unidade</c>), ex.: "UN", "CX".</summary>
    [JsonPropertyName("unidade")]
    public string? Unidade { get; set; }

    /// <summary>Preço unitário do cadastro (<c>valor_unitario</c>).</summary>
    [JsonPropertyName("valor_unitario")]
    public decimal ValorUnitario { get; set; }
}

/// <summary>
/// Parâmetros de <c>UpsertProduto</c>: inclui o produto ou, se o código de integração já existir, altera.
/// Pode rodar de novo sem duplicar.
/// </summary>
public sealed class UpsertProdutoParametros
{
    /// <summary>Código do produto no seu sistema (<c>codigo_produto_integracao</c>). É a chave do upsert.</summary>
    [JsonPropertyName("codigo_produto_integracao")]
    public required string CodigoProdutoIntegracao { get; set; }

    /// <summary>Código do produto (<c>codigo</c>), o SKU visível no Omie.</summary>
    [JsonPropertyName("codigo")]
    public string? Codigo { get; set; }

    /// <summary>Descrição (<c>descricao</c>).</summary>
    [JsonPropertyName("descricao")]
    public required string Descricao { get; set; }

    /// <summary>Sigla da unidade (<c>unidade</c>), ex.: "UN", "CX", "PCT", "FD", "RL".</summary>
    [JsonPropertyName("unidade")]
    public string? Unidade { get; set; }

    /// <summary>NCM (<c>ncm</c>), ex.: "3402.20.00". O Omie exige um NCM válido no cadastro.</summary>
    [JsonPropertyName("ncm")]
    public string? Ncm { get; set; }

    /// <summary>Preço unitário (<c>valor_unitario</c>).</summary>
    [JsonPropertyName("valor_unitario")]
    public decimal? ValorUnitario { get; set; }

    /// <summary>Observações internas (<c>obs_internas</c>).</summary>
    [JsonPropertyName("obs_internas")]
    public string? ObsInternas { get; set; }
}
