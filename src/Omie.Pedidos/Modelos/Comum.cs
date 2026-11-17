using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArrexTec.Omie.Pedidos.Modelos;

/// <summary>
/// Base das respostas do Omie. Os tipos deste pacote trazem só os campos que já usamos de verdade contra a API;
/// tudo o mais que a resposta tiver fica em <see cref="OutrosCampos"/>, sem se perder.
/// </summary>
public abstract class OmieObjeto
{
    /// <summary>Campos da resposta que este pacote ainda não tipou, pelo nome original do Omie.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? OutrosCampos { get; set; }
}

/// <summary>Resposta das chamadas <c>UpsertCliente</c> e <c>UpsertProduto</c>.</summary>
public sealed class UpsertResposta : OmieObjeto
{
    /// <summary>Texto do resultado devolvido pelo Omie (<c>descricao_status</c>).</summary>
    [JsonPropertyName("descricao_status")]
    public string? DescricaoStatus { get; set; }
}
