using System.Text.Json.Serialization;
using ArrexTec.Omie.Pedidos.Json;

namespace ArrexTec.Omie.Pedidos.Modelos;

/// <summary>Parâmetros de <c>ListarContasCorrentes</c> (módulo <c>geral/contacorrente</c>).</summary>
public sealed class ListarContasCorrentesParametros
{
    /// <summary>Página a ler, a partir de 1 (<c>pagina</c>).</summary>
    [JsonPropertyName("pagina")]
    public int Pagina { get; set; } = 1;

    /// <summary>Quantos registros por página (<c>registros_por_pagina</c>). O laboratório usou 50.</summary>
    [JsonPropertyName("registros_por_pagina")]
    public int RegistrosPorPagina { get; set; } = 50;

    /// <summary>Só os registros incluídos pela API (<c>apenas_importado_api</c>, "S"/"N").</summary>
    [JsonPropertyName("apenas_importado_api")]
    [JsonConverter(typeof(SimNaoConverter))]
    public bool ApenasImportadoApi { get; set; }
}

/// <summary>
/// Resposta de <c>ListarContasCorrentes</c>. A lista pode vir em <c>ListarContasCorrentes</c> ou em
/// <c>conta_corrente_lista</c>; use <see cref="Contas"/>, que devolve a que vier.
/// </summary>
public sealed class ListarContasCorrentesResposta : OmieObjeto
{
    /// <summary>Lista no campo <c>ListarContasCorrentes</c>.</summary>
    [JsonPropertyName("ListarContasCorrentes")]
    public List<ContaCorrente>? ListaListarContasCorrentes { get; set; }

    /// <summary>Lista no campo <c>conta_corrente_lista</c>.</summary>
    [JsonPropertyName("conta_corrente_lista")]
    public List<ContaCorrente>? ContaCorrenteLista { get; set; }

    /// <summary>As contas da página, venham no campo que vierem.</summary>
    [JsonIgnore]
    public IReadOnlyList<ContaCorrente> Contas => ListaListarContasCorrentes ?? ContaCorrenteLista ?? [];
}

/// <summary>
/// Conta corrente. O código pode vir em <c>nCodCC</c> ou em <c>codigo_conta_corrente</c>; use
/// <see cref="Codigo"/>, que devolve o que vier. É o código que vai em
/// <see cref="PedidoInformacoesAdicionais.CodigoContaCorrente"/>.
/// </summary>
public sealed class ContaCorrente : OmieObjeto
{
    /// <summary>Código no campo <c>nCodCC</c>.</summary>
    [JsonPropertyName("nCodCC")]
    public long? NCodCC { get; set; }

    /// <summary>Código no campo <c>codigo_conta_corrente</c>.</summary>
    [JsonPropertyName("codigo_conta_corrente")]
    public long? CodigoContaCorrente { get; set; }

    /// <summary>O código da conta, venha no campo que vier.</summary>
    [JsonIgnore]
    public long? Codigo => NCodCC ?? CodigoContaCorrente;
}
