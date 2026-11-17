namespace ArrexTec.Omie.Pedidos;

/// <summary>
/// Configuração do <see cref="OmieClient"/>. A credencial (app_key e app_secret) é do aplicativo que você
/// cadastrou no portal de desenvolvedor do Omie; guarde-a fora do código (variável de ambiente, user-secrets,
/// cofre de segredos).
/// </summary>
public sealed class OmieOptions
{
    /// <summary>Endereço da API v1 do Omie. Cada módulo é um caminho abaixo dele (ex.: <c>produtos/pedido/</c>).</summary>
    public const string EnderecoPadrao = "https://app.omie.com.br/api/v1/";

    /// <summary>Chave do aplicativo no Omie (<c>app_key</c>). Espaços nas pontas são removidos no envio.</summary>
    public string AppKey { get; set; } = "";

    /// <summary>Segredo do aplicativo no Omie (<c>app_secret</c>). Espaços nas pontas são removidos no envio.</summary>
    public string AppSecret { get; set; } = "";

    /// <summary>Endereço base da API. O padrão é <see cref="EnderecoPadrao"/>.</summary>
    public Uri EnderecoBase { get; set; } = new(EnderecoPadrao);

    /// <summary>
    /// Quantas vezes, no total, uma chamada é feita quando o Omie responde com limite de frequência
    /// ("aguarde"/"limite"). O padrão é 3, como no laboratório. Bloqueio por consumo indevido nunca é repetido.
    /// </summary>
    public int MaximoTentativas { get; set; } = 3;

    /// <summary>
    /// Espera antes de repetir uma chamada recusada por limite de frequência, multiplicada pelo número da
    /// tentativa (3 s, depois 6 s, no padrão).
    /// </summary>
    public TimeSpan EsperaPorTentativa { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Desfaz a acentuação trocada que a API às vezes devolve (texto UTF-8 lido como Latin-1, por exemplo
    /// "LaboratÃ³rio" no lugar de "Laboratório"). O dado gravado no Omie está certo; só a resposta vem trocada.
    /// O padrão é <c>true</c>.
    /// </summary>
    public bool CorrigirAcentuacao { get; set; } = true;
}
