using System.Text.Json.Nodes;

namespace Omie.Pedidos.Verificacao;

/// <summary>Falha de uma conferência.</summary>
internal sealed class FalhaVerificacao(string mensagem) : Exception(mensagem);

/// <summary>Conferências mínimas, sem biblioteca de teste.</summary>
internal static class Conferir
{
    public static void Igual<T>(T esperado, T atual, string oQue)
    {
        if (!EqualityComparer<T>.Default.Equals(esperado, atual))
        {
            throw new FalhaVerificacao($"{oQue}: esperado [{esperado}], veio [{atual}]");
        }
    }

    public static void Verdadeiro(bool condicao, string oQue)
    {
        if (!condicao)
        {
            throw new FalhaVerificacao(oQue);
        }
    }

    /// <summary>Confere o texto de um campo do JSON enviado.</summary>
    public static void Texto(string esperado, JsonNode? no, string campo) =>
        Igual(esperado, no?.GetValue<string>(), campo);

    /// <summary>Confere o número de um campo do JSON enviado.</summary>
    public static void Numero(decimal esperado, JsonNode? no, string campo) =>
        Igual(esperado, no?.GetValue<decimal>(), campo);

    /// <summary>Confere que o campo não foi enviado.</summary>
    public static void Ausente(JsonObject objeto, string campo) =>
        Verdadeiro(!objeto.ContainsKey(campo), $"{campo} não devia ser enviado");

    /// <summary>Confere que a ação lança <typeparamref name="TExcecao"/> e devolve a exceção.</summary>
    public static async Task<TExcecao> LancaAsync<TExcecao>(Func<Task> acao, string oQue)
        where TExcecao : Exception
    {
        try
        {
            await acao();
        }
        catch (TExcecao ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            throw new FalhaVerificacao($"{oQue}: esperado {typeof(TExcecao).Name}, veio {ex.GetType().Name}: {ex.Message}");
        }

        throw new FalhaVerificacao($"{oQue}: esperado {typeof(TExcecao).Name}, não lançou nada");
    }
}
