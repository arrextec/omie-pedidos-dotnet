using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace ArrexTec.Omie.Pedidos.Json;

/// <summary>Configuração de JSON usada nas chamadas ao Omie.</summary>
internal static class OmieJson
{
    /// <summary>
    /// Campos nulos não são enviados (o Omie recebe só o que foi preenchido); números que chegam como texto
    /// são aceitos na leitura; acentos vão como estão, sem <c>\uXXXX</c>.
    /// </summary>
    public static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.General)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    // Leitor estrito: só aceita a troca se os bytes formarem UTF-8 válido
    private static readonly UTF8Encoding Utf8Estrito = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Desfaz, em todas as strings do JSON, a troca de UTF-8 lido como Latin-1 ("LaboratÃ³rio" → "Laboratório").
    /// Só troca quando o resultado é UTF-8 válido, para não estragar um "SÃO PAULO" que já veio certo.
    /// </summary>
    public static void CorrigirAcentuacao(JsonNode? no)
    {
        switch (no)
        {
            case JsonObject objeto:
                foreach (var chave in objeto.Select(p => p.Key).ToList())
                {
                    var filho = objeto[chave];
                    if (filho is JsonValue valor && valor.TryGetValue<string>(out var texto))
                    {
                        if (TentarCorrigir(texto, out var corrigido))
                        {
                            objeto[chave] = JsonValue.Create(corrigido);
                        }
                    }
                    else
                    {
                        CorrigirAcentuacao(filho);
                    }
                }
                break;

            case JsonArray lista:
                for (var i = 0; i < lista.Count; i++)
                {
                    var filho = lista[i];
                    if (filho is JsonValue valor && valor.TryGetValue<string>(out var texto))
                    {
                        if (TentarCorrigir(texto, out var corrigido))
                        {
                            lista[i] = JsonValue.Create(corrigido);
                        }
                    }
                    else
                    {
                        CorrigirAcentuacao(filho);
                    }
                }
                break;
        }
    }

    /// <summary>Tenta desfazer a troca de codificação de um texto.</summary>
    internal static bool TentarCorrigir(string texto, out string corrigido)
    {
        corrigido = texto;
        // o sinal da troca é "Ã" ou "Â" seguido do segundo byte; e todo caractere precisa caber em Latin-1
        if (texto.IndexOfAny(['Ã', 'Â']) < 0 || texto.Any(c => c > 'ÿ'))
        {
            return false;
        }

        try
        {
            corrigido = Utf8Estrito.GetString(Encoding.Latin1.GetBytes(texto));
            return corrigido != texto;
        }
        catch (DecoderFallbackException)
        {
            // não era troca de codificação: o texto já veio certo
            corrigido = texto;
            return false;
        }
    }
}

/// <summary>Converte <c>bool</c> para o "S"/"N" que o Omie usa nos campos de sim/não.</summary>
internal sealed class SimNaoConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.String => string.Equals(reader.GetString(), "S", StringComparison.OrdinalIgnoreCase),
            _ => throw new JsonException("Esperado \"S\" ou \"N\"."),
        };

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value ? "S" : "N");
}

/// <summary>Converte <see cref="DateOnly"/> para a data no formato do Omie (dd/MM/aaaa).</summary>
internal sealed class DataOmieConverter : JsonConverter<DateOnly>
{
    private const string Formato = "dd/MM/yyyy";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        DateOnly.ParseExact(reader.GetString() ?? "", Formato, CultureInfo.InvariantCulture);

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(Formato, CultureInfo.InvariantCulture));
}

/// <summary>Lê como texto um campo que o Omie pode devolver como texto ou como número.</summary>
internal sealed class TextoOuNumeroConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => Encoding.UTF8.GetString(reader.ValueSpan),
            JsonTokenType.Null => null,
            _ => throw new JsonException("Esperado texto ou número."),
        };

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
