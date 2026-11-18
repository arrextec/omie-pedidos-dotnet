# ArrexTec.Omie.Pedidos

Cliente .NET tipado da API oficial do Omie para **lançar pedidos de venda**: o pedido que chegou por e-mail, PDF,
planilha ou WhatsApp, depois de lido e conferido, entra no Omie pela API, e não por digitação.

O pacote cobre o caminho que o pedido percorre até o ERP: ler o cadastro de clientes e produtos, escolher a conta
corrente, gravar o pedido de venda e tratar a recusa da API. Não tem dependência externa: só `HttpClient` e
`System.Text.Json`, do próprio .NET.

> Projeto da ArrexTec, sem vínculo com a Omie. "Omie" é marca de seu titular e aparece aqui só para dizer com
> que API o pacote fala.
>
> Quer o agente que lê o pedido e lança no Omie sem você escrever o código? Veja
> [Agente de pedido para Omie](https://arrextec.com.br/agente-de-pedido-omie/).

## Situação

| Item | Situação |
|---|---|
| Código | versão 0.1.0, em revisão |
| Pacote NuGet | **ainda não publicado**: use o build local (abaixo) |
| Repositório público | proposto em `github.com/arrextec/omie-pedidos-dotnet`, ainda não criado |
| Licença | **MIT proposta**, depende do OK do Vitor antes de publicar ([LICENSE](LICENSE)) |

## Instalação (build local)

Requer o SDK do .NET 10.

```bash
git clone <este repositório>
cd omie-pedidos-dotnet
dotnet build
dotnet run --project tests/Omie.Pedidos.Verificacao   # verificação sem rede; sai com 0 se tudo passar
```

No seu projeto, referencie a biblioteca direto:

```xml
<ItemGroup>
  <ProjectReference Include="..\omie-pedidos-dotnet\src\Omie.Pedidos\Omie.Pedidos.csproj" />
</ItemGroup>
```

Quando o pacote for publicado, será `dotnet add package ArrexTec.Omie.Pedidos`.

## Exemplo de uso

A credencial (`app_key` e `app_secret`) é a chave de acesso do **seu** aplicativo Omie, e só um usuário
administrador do Omie consegue obtê-la. Guarde-a fora do código: variável de ambiente, `dotnet user-secrets` ou
cofre de segredos.

```csharp
using ArrexTec.Omie.Pedidos;
using ArrexTec.Omie.Pedidos.Modelos;

var omie = new OmieClient(new HttpClient(), new OmieOptions
{
    AppKey = Environment.GetEnvironmentVariable("OMIE_APP_KEY")!,
    AppSecret = Environment.GetEnvironmentVariable("OMIE_APP_SECRET")!,
});

// 1. cadastro do ERP: clientes, produtos e a conta corrente do pedido
var clientes = await omie.ListarClientesAsync();          // página 1, 100 registros
var produtos = await omie.ListarProdutosAsync();
var contas = await omie.ListarContasCorrentesAsync();

// 2. o pedido lido e conferido vira um pedido de venda no Omie
var cliente = clientes.Clientes!.First(c => c.CnpjCpf == "00.000.000/0001-91");
var produto = produtos.Produtos!.First(p => p.Codigo == "DET-500");

try
{
    var resposta = await omie.IncluirPedidoAsync(new IncluirPedidoParametros
    {
        Cabecalho = new PedidoCabecalho
        {
            CodigoCliente = cliente.CodigoClienteOmie,
            CodigoPedidoIntegracao = "PED-2026-0001",   // único por pedido, no seu sistema
            DataPrevisao = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),   // igual ou posterior a hoje
            Etapa = "10",
            CodigoParcela = "000",   // condição de pagamento: use o código lido em ListarFormasPagVendas da sua conta
                                     // (produtos/formaspagvendas); "000" é só o valor do laboratório
        },
        Itens =
        [
            new PedidoItem
            {
                Ide = new PedidoItemIde { CodigoItemIntegracao = "1" },
                Produto = new PedidoItemProduto
                {
                    CodigoProduto = produto.CodigoProduto,
                    Quantidade = 12,
                    ValorUnitario = produto.ValorUnitario,   // o preço é o da sua tabela, não o que veio escrito
                },
            },
        ],
        InformacoesAdicionais = new PedidoInformacoesAdicionais
        {
            CodigoCategoria = "1.01.01",   // código lido em ListarCategorias (geral/categorias); varia por empresa
            CodigoContaCorrente = contas.Contas[0].Codigo,
        },
        Observacoes = new PedidoObservacoes { ObsVenda = "Pedido recebido por e-mail e conferido." },
    });

    Console.WriteLine($"Pedido nº {resposta.NumeroPedido} gravado (código {resposta.CodigoPedido}).");
}
catch (OmieException ex) when (ex.Tipo == OmieErroTipo.Bloqueio)
{
    // a API bloqueou a chamada por consumo indevido (30 min, pela documentação da Omie): pare e tente mais tarde,
    // sem insistir
}
catch (OmieException ex)
{
    Console.WriteLine($"O Omie recusou: {ex.FaultString} ({ex.FaultCode})");
}
```

Com `IHttpClientFactory`:

```csharp
services.AddHttpClient<OmieClient>();
services.AddSingleton(new OmieOptions { AppKey = "...", AppSecret = "..." });
```

### Chamadas cobertas

| Método | Módulo | `call` |
|---|---|---|
| `ListarClientesResumidoAsync` | `geral/clientes` | `ListarClientesResumido` |
| `ListarClientesAsync` | `geral/clientes` | `ListarClientes` |
| `ConsultarClienteAsync` | `geral/clientes` | `ConsultarCliente` |
| `UpsertClienteAsync` | `geral/clientes` | `UpsertCliente` |
| `ListarProdutosResumidoAsync` | `geral/produtos` | `ListarProdutosResumido` |
| `ListarProdutosAsync` | `geral/produtos` | `ListarProdutos` |
| `UpsertProdutoAsync` | `geral/produtos` | `UpsertProduto` |
| `ListarContasCorrentesAsync` | `geral/contacorrente` | `ListarContasCorrentes` |
| `IncluirPedidoAsync` | `produtos/pedido` | `IncluirPedido` |

Para qualquer outra chamada, use `ChamarAsync<TResposta>(modulo, call, parametros)`: o envelope, a credencial, o
tratamento de erro e a nova tentativa são os mesmos. `TResposta` pode ser `JsonNode` enquanto o tipo não existe.

Os tipos trazem só os campos que já usamos de verdade contra a API. O que mais a resposta tiver fica em
`OutrosCampos` (um dicionário pelo nome original do Omie), sem se perder.

### Convenção de nomes

Tipos de infraestrutura seguem o .NET (`OmieClient`, `OmieOptions`, `OmieException`). O que espelha o Omie fica em
português, como na API: métodos com o nome da `call` (`IncluirPedidoAsync`), parâmetros e respostas com o nome da
`call` (`IncluirPedidoParametros`, `IncluirPedidoResposta`) e propriedades em PascalCase com o nome original no
`[JsonPropertyName]` (`CodigoPedidoIntegracao` ↔ `codigo_pedido_integracao`). Campos "S"/"N" viram `bool`, e
`data_previsao` vira `DateOnly` (enviada como dd/MM/aaaa; a Omie exige data igual ou posterior à data corrente).

## Erros e limites da API

No laboratório, a recusa veio como um JSON `{ "faultstring": "...", "faultcode": "..." }`, com HTTP 500. A
documentação da Omie cita o HTTP 500 para parâmetro preenchido de forma incorreta e descreve também a estrutura de
erro `omie_fail` (`code`, `description`, `referer`, `fatal`). O cliente transforma a recusa com `faultstring` em
`OmieException`, com `FaultString`, `FaultCode`, `StatusHttp`, `Modulo`, `Call` e `Tipo`. Erro HTTP sem
`faultstring` (por exemplo, no formato `omie_fail`) vira `RespostaInvalida`, com o `StatusHttp`. A credencial
nunca entra na mensagem do erro.

| `Tipo` | Quando | O que o cliente faz |
|---|---|---|
| `Api` | erro de negócio ou validação (campo faltando, cadastro inexistente) | lança na hora |
| `Bloqueio` | "bloqueada" / "consumo indevido" | lança na hora, **sem repetir**. Pela documentação da Omie, na 10ª requisição com erro para o mesmo IP + App Key + método, a API bloqueia por 30 minutos (HTTP 425). Não repita durante o bloqueio |
| `ConsumoRedundante` | consulta ao mesmo registro repetida dentro de 60 segundos | lança na hora: use o resultado anterior |
| `LimiteFrequencia` | "aguarde" / "limite" (textos vistos no laboratório) | espera e tenta de novo: 3 tentativas no total, com 3 s e 6 s de espera (`MaximoTentativas`, `EsperaPorTentativa`) |
| `RespostaInvalida` | resposta que não é JSON, ou HTTP de erro sem `faultstring` | lança na hora |

A mensagem que a Omie documenta para o limite de consumo ultrapassado é **"Too many requests"**. Esta versão ainda
não a classifica: ela cai em `Api` (se vier na `faultstring`) ou em `RespostaInvalida`, sempre sem nova tentativa.
O mesmo vale para o HTTP 425 do bloqueio quando ele vier sem `faultstring`. Ver Pendências.

Limites da API, pela documentação da Omie ([limites de consumo](https://ajuda.omie.com.br/pt-BR/articles/8112984-limites-de-consumo-da-api-do-omie)
e [tratando os erros](https://ajuda.omie.com.br/pt-BR/articles/8001888-tratando-os-erros-de-api)), e o que o
laboratório mostrou, marcado como tal:

- **Aplicativo Trial: 60 requisições por minuto e 100 por dia** (aplicativo FIT: 500 por dia), e o aplicativo de
  teste vale 7 dias. Planeje a carga e a medição em cima disso: leia o cadastro uma vez e guarde, e não liste de
  novo a cada pedido.
- **Respeite o limite por minuto e não faça inclusões simultâneas.** A Omie limita a 240 requisições por minuto por
  IP + App Key + método e não aceita requisições simultâneas de inclusão ou alteração. O bloqueio de 30 minutos
  vem de outra coisa: repetir requisição com erro. No laboratório, as chamadas foram espaçadas de 0,4 s a 1,5 s;
  este pacote não impõe intervalo, e quem chama decide o ritmo.
- **Antes de cadastrar, liste o que já existe** e só envie o que falta. A Omie documenta o código de integração do
  produto para evitar cadastro duplicado, e é ele a chave do `UpsertProduto`. Em `UpsertCliente`, não duplicar foi
  o que observamos no laboratório; a Omie também tem o `UpsertClienteCpfCnpj`, que faz o upsert pelo CPF/CNPJ.
- **Acentuação trocada na resposta** (observado no laboratório, não documentado pela Omie). A API às vezes devolveu
  texto UTF-8 lido como Latin-1 ("LaboratÃ³rio"). O dado gravado estava certo; o cliente desfaz a troca na resposta
  (`CorrigirAcentuacao`, ligado por padrão) e não estraga um texto que já veio certo. O que a documentação da Omie
  descreve é outra coisa: a conversão de caracteres especiais (`&`, `<`, `>`, `'`, `"` e `|`), como `&` em `&amp;`.
- A classificação do erro é pelo texto da `faultstring`, porque é o que a API devolveu no laboratório; se a Omie
  mudar o texto, a recusa cai em `Api` e é lançada sem nova tentativa, que é o lado seguro.

## O método do laboratório

Este pacote nasceu do laboratório de pedido da ArrexTec, que testa o desenho do agente de entrada de pedido de
ponta a ponta, sem dado de cliente:

1. **Uma empresa fictícia**: distribuidora de material de limpeza e descartáveis, com catálogo de 24 itens, 10
   clientes, duas tabelas de preço e desconto padrão para alguns clientes, tudo cadastrado num Omie de sandbox pela
   API (`UpsertProduto`, `UpsertCliente`).
2. **50 pedidos como os compradores mandam de verdade**: 30 em PDF (três layouts), 10 em planilha e 10 por WhatsApp,
   cada cliente escrevendo o item do seu jeito. Os pedidos têm problemas de propósito (preço diferente da tabela,
   unidade errada, item ambíguo, item fora do catálogo, pedido repetido), e um gabarito diz o que cada um tem.
3. **O agente** lê o pedido, casa cliente (pelo CNPJ) e itens com o cadastro lido do Omie, confere unidade, preço
   e duplicidade, e **grava pela API** (`IncluirPedido`) o que está no padrão. O que foge do padrão vai para a fila
   de uma pessoa, com o motivo. O preço gravado é sempre o da tabela do ERP, nunca o que veio escrito.
4. **A medição** compara cada decisão com o gabarito, só depois da rodada.

Em nosso ambiente de teste, num Omie de sandbox, com pedidos gerados pela própria ArrexTec:

| Número | Valor |
|---|---|
| Pedidos gravados no Omie, na rodada que gravou | 26 de 27, todos aceitos pela API |
| Tempo médio por pedido, com a gravação no Omie (nos 27 pedidos dessa rodada) | 18 s (de 14 a 24 s) |
| Campos extraídos sem correção (nos 50 pedidos) | 98% |
| Decisão certa (lançou certo, ou mandou para a fila quando devia) | 49 de 50 |

O pedido que mudou de decisão foi um item ambíguo que o agente mandou para a fila: o erro foi para o lado seguro,
nada errado entrou no ERP. É prova do desenho (ler, conferir no ERP, mandar a exceção para uma pessoa), não
promessa de percentual.

Mais sobre o agente: [arrextec.com.br/agente-de-pedido-omie](https://arrextec.com.br/agente-de-pedido-omie/).

## Estrutura

```
omie-pedidos-dotnet/
├── src/Omie.Pedidos/               biblioteca (net10.0, sem dependência externa)
│   ├── OmieClient.cs               cliente, chamadas tipadas e ChamarAsync genérico
│   ├── OmieOptions.cs              credencial, endereço, nova tentativa, correção de acentuação
│   ├── OmieException.cs            erro da API (faultstring/faultcode) e OmieErroTipo
│   ├── Json/OmieJson.cs            JSON do Omie: "S"/"N", dd/MM/aaaa, texto ou número, acentuação
│   └── Modelos/                    parâmetros e respostas: clientes, produtos, contas correntes, pedidos
└── tests/Omie.Pedidos.Verificacao/ console de verificação com HttpMessageHandler falso (sem rede)
```

## Pendências (TODO)

- **`ConsultarPedido` e `ListarPedidos`**: o laboratório só incluiu pedidos e não consultou nenhum de volta pela
  API, então não há chamada nem campo validado para tipar. Até lá, use `ChamarAsync<JsonNode>`.
- **Paginação**: as listagens leem uma página (padrão 100 registros). O laboratório coube numa página e não usou os
  campos de total de páginas da resposta; um iterador de todas as páginas fica para quando for validado.
- **Outras chamadas do pedido** (`AlterarPedidoVenda`, `TrocarEtapaPedido`, `CancelarPedidoVenda`,
  `FaturarPedidoVenda`): fora do que o laboratório usou.
- **Texto exato das mensagens de limite**: a classificação usa os trechos que o laboratório viu ("bloqueada",
  "consumo indevido", "redundante", "aguarde", "limite"). Falta classificar a mensagem oficial "Too many requests"
  e o HTTP 425 do bloqueio, e ler a estrutura `omie_fail`. Os valores de `faultcode` não foram observados nos
  scripts; o campo é exposto, mas nada decide por ele. Se o limite diário de 100 chamadas do app de teste vier com
  a palavra "limite", o cliente tenta de novo antes de desistir (2 chamadas a mais).
- **Campos da resposta de `IncluirPedido`**: tipados só `codigo_pedido` e `numero_pedido`; o resto fica em
  `OutrosCampos`.
- **Publicação**: repositório no GitHub, pacote no NuGet, CI de build e verificação.

## Licença

MIT ([LICENSE](LICENSE)), Copyright (c) 2026 ArrexTec. **Proposta: depende do OK do Vitor antes de publicar.**
