using System.Text.Json.Serialization;
using ArrexTec.Omie.Pedidos.Json;

namespace ArrexTec.Omie.Pedidos.Modelos;

/// <summary>Parâmetros de <c>ListarClientes</c> e <c>ListarClientesResumido</c> (módulo <c>geral/clientes</c>).</summary>
public sealed class ListarClientesParametros
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
}

/// <summary>Resposta de <c>ListarClientesResumido</c>.</summary>
public sealed class ListarClientesResumidoResposta : OmieObjeto
{
    /// <summary>Clientes da página (<c>clientes_cadastro_resumido</c>).</summary>
    [JsonPropertyName("clientes_cadastro_resumido")]
    public List<ClienteResumido>? Clientes { get; set; }
}

/// <summary>Cliente na listagem resumida.</summary>
public sealed class ClienteResumido : OmieObjeto
{
    /// <summary>Razão social (<c>razao_social</c>).</summary>
    [JsonPropertyName("razao_social")]
    public string? RazaoSocial { get; set; }
}

/// <summary>Resposta de <c>ListarClientes</c>.</summary>
public sealed class ListarClientesResposta : OmieObjeto
{
    /// <summary>Clientes da página (<c>clientes_cadastro</c>).</summary>
    [JsonPropertyName("clientes_cadastro")]
    public List<ClienteCadastro>? Clientes { get; set; }
}

/// <summary>
/// Cliente como o Omie devolve em <c>ListarClientes</c> e em <c>ConsultarCliente</c>. A listagem não traz a
/// observação: para ela, consulte o cliente.
/// </summary>
public sealed class ClienteCadastro : OmieObjeto
{
    /// <summary>Código do cliente no Omie (<c>codigo_cliente_omie</c>). É o que vai no pedido.</summary>
    [JsonPropertyName("codigo_cliente_omie")]
    public long CodigoClienteOmie { get; set; }

    /// <summary>Código do cliente no seu sistema (<c>codigo_cliente_integracao</c>).</summary>
    [JsonPropertyName("codigo_cliente_integracao")]
    public string? CodigoClienteIntegracao { get; set; }

    /// <summary>Razão social (<c>razao_social</c>).</summary>
    [JsonPropertyName("razao_social")]
    public string? RazaoSocial { get; set; }

    /// <summary>CNPJ ou CPF, como está no cadastro (<c>cnpj_cpf</c>).</summary>
    [JsonPropertyName("cnpj_cpf")]
    public string? CnpjCpf { get; set; }

    /// <summary>Observação do cadastro (<c>observacao</c>); vem em <c>ConsultarCliente</c>.</summary>
    [JsonPropertyName("observacao")]
    public string? Observacao { get; set; }
}

/// <summary>Parâmetros de <c>ConsultarCliente</c>.</summary>
public sealed class ConsultarClienteParametros
{
    /// <summary>Código do cliente no Omie (<c>codigo_cliente_omie</c>).</summary>
    [JsonPropertyName("codigo_cliente_omie")]
    public required long CodigoClienteOmie { get; set; }
}

/// <summary>
/// Parâmetros de <c>UpsertCliente</c>: inclui o cliente ou, se o código de integração já existir, altera.
/// Pode rodar de novo sem duplicar.
/// </summary>
public sealed class UpsertClienteParametros
{
    /// <summary>Código do cliente no seu sistema (<c>codigo_cliente_integracao</c>). É a chave do upsert.</summary>
    [JsonPropertyName("codigo_cliente_integracao")]
    public required string CodigoClienteIntegracao { get; set; }

    /// <summary>Razão social (<c>razao_social</c>).</summary>
    [JsonPropertyName("razao_social")]
    public required string RazaoSocial { get; set; }

    /// <summary>Nome fantasia (<c>nome_fantasia</c>).</summary>
    [JsonPropertyName("nome_fantasia")]
    public string? NomeFantasia { get; set; }

    /// <summary>CNPJ ou CPF (<c>cnpj_cpf</c>). O Omie exige dígito verificador válido.</summary>
    [JsonPropertyName("cnpj_cpf")]
    public string? CnpjCpf { get; set; }

    /// <summary>Cidade (<c>cidade</c>). O laboratório usou o formato "CAMPINAS (SP)".</summary>
    [JsonPropertyName("cidade")]
    public string? Cidade { get; set; }

    /// <summary>UF (<c>estado</c>), ex.: "SP".</summary>
    [JsonPropertyName("estado")]
    public string? Estado { get; set; }

    /// <summary>Pessoa física (<c>pessoa_fisica</c>, "S"/"N"). O padrão é "N".</summary>
    [JsonPropertyName("pessoa_fisica")]
    [JsonConverter(typeof(SimNaoConverter))]
    public bool PessoaFisica { get; set; }

    /// <summary>Observação do cadastro (<c>observacao</c>).</summary>
    [JsonPropertyName("observacao")]
    public string? Observacao { get; set; }

    /// <summary>Etiquetas do cliente (<c>tags</c>).</summary>
    [JsonPropertyName("tags")]
    public List<ClienteTag>? Tags { get; set; }
}

/// <summary>Etiqueta de cliente (<c>{ "tag": "..." }</c>).</summary>
public sealed class ClienteTag
{
    /// <summary>Cria uma etiqueta vazia.</summary>
    public ClienteTag()
    {
    }

    /// <summary>Cria a etiqueta com o texto informado.</summary>
    /// <param name="tag">Texto da etiqueta.</param>
    public ClienteTag(string tag) => Tag = tag;

    /// <summary>Texto da etiqueta (<c>tag</c>).</summary>
    [JsonPropertyName("tag")]
    public string Tag { get; set; } = "";
}
