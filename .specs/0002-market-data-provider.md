# SPEC 0002 — Market Data Provider

## 1. Visão geral

Esta SPEC complementa a `0001-market-radar-web-mvp.md` e define como a aplicação Market Radar obterá dados de mercado para análise técnica.

O foco desta SPEC é responder uma pergunta específica:

```text
Como a aplicação obtém candles confiáveis para análise?
```

A SPEC 0002 não deve tratar de dashboard, layout web, cálculo de regressão linear, cálculo de EMAs ou regras de alerta. Esses assuntos pertencem à SPEC 0001 e às SPECs futuras de análise técnica.

---

## 2. Objetivo

Definir uma camada de abstração para obtenção de candles de mercado, permitindo que a aplicação utilize diferentes fontes de dados sem alterar as regras de análise.

A aplicação deverá suportar inicialmente um provider mockado para desenvolvimento e testes, e deverá estar preparada para providers reais no futuro, como MetaTrader, FotMarket ou outro provedor externo.

---

## 3. Escopo desta SPEC

Esta SPEC cobre:

1. Contrato de provider de mercado.
2. Modelo padronizado de candle.
3. Normalização de símbolos.
4. Normalização de timeframe.
5. Normalização de data/hora.
6. Tratamento de falhas.
7. Deduplicação de candles.
8. Provider mockado.
9. Estratégia para providers reais futuros.
10. Critérios de aceite da camada de dados.

---

## 4. Fora do escopo desta SPEC

Esta SPEC não cobre:

1. Dashboard web.
2. Gráficos.
3. Alertas visuais ou sonoros.
4. Regressão linear.
5. Médias móveis.
6. Score de alerta.
7. IA.
8. Backtest.
9. Execução de ordens.
10. Estratégias de compra ou venda.

---

## 5. Decisão arquitetural

A aplicação deverá depender de uma abstração chamada `IMarketDataProvider`.

Nenhuma camada de análise técnica deverá acessar diretamente uma API externa, arquivo externo, MetaTrader, FotMarket ou qualquer outro serviço de mercado.

O fluxo correto será:

```text
Provider externo
↓
IMarketDataProvider
↓
Normalização dos candles
↓
Persistência
↓
Serviços de análise técnica
```

---

## 6. Contrato principal

## 6.1 IMarketDataProvider

```csharp
public interface IMarketDataProvider
{
    Task<IReadOnlyCollection<MarketCandle>> GetLatestCandlesAsync(
        string symbol,
        string timeframe,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken);
}
```

### Regras do contrato

1. O método deve retornar candles no intervalo solicitado.
2. Os candles devem estar normalizados no formato interno da aplicação.
3. O provider não deve salvar candles no banco.
4. O provider não deve executar análise técnica.
5. O provider não deve emitir alertas.
6. O provider deve lançar exceções controladas ou retornar erro tratável em caso de falha externa.
7. O provider deve respeitar o `CancellationToken`.

---

## 7. Modelo padronizado de candle

O provider deverá retornar candles no modelo interno da aplicação.

```csharp
public class MarketCandle
{
    public long Id { get; set; }
    public string Symbol { get; set; }
    public string Timeframe { get; set; }
    public DateTime OpenTime { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal? Volume { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

## 7.1 Regras do candle

1. `Symbol` deve estar normalizado.
2. `Timeframe` deve estar normalizado.
3. `OpenTime` deve representar o início do candle.
4. `OpenPrice`, `HighPrice`, `LowPrice` e `ClosePrice` são obrigatórios.
5. `Volume` poderá ser nulo caso o provider não forneça volume.
6. `CreatedAt` deve ser preenchido pela aplicação ao persistir, não necessariamente pelo provider.

---

## 8. Timeframes suportados

No MVP, o timeframe obrigatório será:

```text
M1 — candle de 1 minuto
```

Timeframes futuros:

```text
M5
M15
M30
H1
H4
D1
```

## 8.1 Normalização de timeframe

A aplicação deverá trabalhar internamente com os seguintes códigos:

```csharp
public static class MarketTimeframes
{
    public const string M1 = "M1";
    public const string M5 = "M5";
    public const string M15 = "M15";
    public const string M30 = "M30";
    public const string H1 = "H1";
    public const string H4 = "H4";
    public const string D1 = "D1";
}
```

Cada provider poderá usar códigos próprios, mas deverá converter para o padrão interno.

Exemplo:

```text
Provider externo: 1m
Padrão interno: M1
```

---

## 9. Símbolos suportados

No MVP, o símbolo inicial será:

```text
XAUUSD
```

A aplicação deverá permitir configuração futura de múltiplos símbolos.

## 9.1 Normalização de símbolo

O provider deverá converter símbolos externos para o padrão interno.

Exemplos possíveis:

```text
XAUUSD    → XAUUSD
XAU/USD   → XAUUSD
GOLD      → XAUUSD, se configurado explicitamente
BTCUSDT   → BTCUSDT
BTC/USDT  → BTCUSDT
```

A normalização não deve ser baseada em adivinhação frágil. Quando houver ambiguidade, deverá existir configuração explícita.

Exemplo de configuração futura:

```json
{
  "SymbolMappings": [
    {
      "InternalSymbol": "XAUUSD",
      "ProviderSymbol": "XAUUSD"
    },
    {
      "InternalSymbol": "XAUUSD",
      "ProviderSymbol": "GOLD"
    }
  ]
}
```

---

## 10. Data/hora e timezone

Todos os candles deverão ser armazenados com data/hora padronizada.

Decisão recomendada:

```text
Persistir OpenTime em UTC.
```

## 10.1 Regras

1. Providers que retornam horário local devem ser convertidos para UTC.
2. Providers que retornam timestamp Unix devem ser convertidos para `DateTime` UTC.
3. O dashboard poderá converter UTC para horário local do usuário.
4. A deduplicação deverá usar o `OpenTime` normalizado.

## 10.2 Risco

Timezone errado pode gerar candles duplicados, buracos no histórico ou análise incorreta.

Por isso, a normalização de horário deve ser tratada como parte obrigatória do provider.

---

## 11. Persistência e deduplicação

O provider não será responsável por persistir candles.

A persistência será feita por um repositório, por exemplo:

```csharp
public interface ICandleRepository
{
    Task AddOrUpdateAsync(
        IReadOnlyCollection<MarketCandle> candles,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<MarketCandle>> GetRecentAsync(
        string symbol,
        string timeframe,
        int count,
        CancellationToken cancellationToken);
}
```

## 11.1 Chave lógica do candle

Um candle será considerado único pela combinação:

```text
Symbol + Timeframe + OpenTime
```

## 11.2 Regra de deduplicação

Se o worker buscar novamente um candle já existente, a aplicação não deverá duplicar o registro.

Estratégia recomendada:

1. Criar índice único no banco.
2. Implementar `AddOrUpdateAsync`.
3. Permitir atualização de candle ainda em formação.

## 11.3 Candle fechado versus candle em formação

Um ponto importante: o candle atual de M1 pode ainda estar em formação.

Exemplo:

```text
Agora: 10:35:27
Candle M1 atual: 10:35:00 ainda não fechou
Último candle fechado: 10:34:00
```

Para análise técnica, a recomendação inicial é:

```text
Usar preferencialmente candles fechados.
```

Isso reduz falso sinal.

---

## 12. Provider mockado

O MVP deverá possuir um `MockMarketDataProvider`.

Objetivo:

1. Permitir desenvolvimento sem depender de API externa.
2. Permitir testes previsíveis.
3. Simular tendências de alta.
4. Simular tendências de baixa.
5. Simular rompimentos.
6. Simular falhas de provider.
7. Simular ausência de candles.

## 12.1 Contrato do mock

```csharp
public class MockMarketDataProvider : IMarketDataProvider
{
    public Task<IReadOnlyCollection<MarketCandle>> GetLatestCandlesAsync(
        string symbol,
        string timeframe,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        // Retorna candles simulados.
    }
}
```

## 12.2 Cenários simulados

O mock deverá permitir ao menos os seguintes cenários:

```text
StableMarket
UpTrend
DownTrend
TrendBreakDown
TrendBreakUp
ProviderFailure
EmptyResponse
```

Exemplo de configuração:

```json
{
  "MarketDataProvider": {
    "ProviderType": "Mock",
    "MockScenario": "TrendBreakDown"
  }
}
```

---

## 13. Providers reais futuros

A aplicação deverá estar preparada para múltiplos providers.

Implementações previstas:

```text
MockMarketDataProvider
MetaTraderMarketDataProvider
FotMarketDataProvider
ExternalHttpMarketDataProvider
CsvMarketDataProvider
```

## 13.1 MetaTraderMarketDataProvider

Provider futuro para integração com MetaTrader.

Pontos a avaliar:

1. Forma de acesso aos candles.
2. Se a integração será via arquivo exportado.
3. Se a integração será via script/EA.
4. Se haverá ponte HTTP local.
5. Se haverá dependência do terminal aberto.
6. Como lidar com autenticação e sessão.

## 13.2 FotMarketDataProvider

Provider futuro para integração específica com FotMarket, caso exista API disponível.

Pontos a avaliar:

1. Existência de API pública.
2. Documentação oficial.
3. Autenticação.
4. Limites de requisição.
5. Disponibilidade de candles M1.
6. Timezone usado pela plataforma.
7. Formato do símbolo para ouro.

Se não houver API pública confiável, não se deve forçar integração direta com FotMarket.

## 13.3 ExternalHttpMarketDataProvider

Provider genérico para consumir dados de uma API externa de mercado.

Deverá ser configurável por:

1. URL base.
2. Token ou API key.
3. Formato de símbolo.
4. Conversão de timeframe.
5. Mapeamento do JSON externo para `MarketCandle`.

## 13.4 CsvMarketDataProvider

Provider útil para testes, backtests simples e desenvolvimento offline.

Deverá ler candles a partir de arquivo CSV.

Campos mínimos esperados:

```text
Symbol
Timeframe
OpenTime
OpenPrice
HighPrice
LowPrice
ClosePrice
Volume
```

---

## 14. Configuração do provider

Exemplo inicial no `appsettings.json`:

```json
{
  "MarketDataProvider": {
    "ProviderType": "Mock",
    "MockScenario": "TrendBreakDown",
    "UseOnlyClosedCandles": true,
    "RequestOverlapMinutes": 5
  }
}
```

## 14.1 ProviderType

Valores previstos:

```text
Mock
MetaTrader
FotMarket
ExternalHttp
Csv
```

## 14.2 UseOnlyClosedCandles

Quando habilitado, a aplicação deverá ignorar candle ainda em formação.

Valor recomendado para o MVP:

```text
true
```

## 14.3 RequestOverlapMinutes

Define uma pequena sobreposição na busca de candles para evitar buracos causados por atraso de provider.

Exemplo:

```text
Última busca: 10:30 até 10:35
Próxima busca com overlap: 10:30 até 10:40
```

A deduplicação impedirá registros duplicados.

Valor recomendado para o MVP:

```text
5
```

---

## 15. Tratamento de falhas

Falhas no provider não devem derrubar a aplicação.

## 15.1 Falhas previstas

1. Provider indisponível.
2. Timeout.
3. Resposta vazia.
4. Candle incompleto.
5. Símbolo inválido.
6. Timeframe não suportado.
7. Erro de autenticação.
8. Limite de requisições.
9. Dados fora de ordem.
10. Dados duplicados.

## 15.2 Comportamento esperado

Quando ocorrer falha:

1. Registrar log de erro.
2. Não interromper o worker.
3. Não gerar alerta técnico baseado em dados incompletos.
4. Tentar novamente no próximo ciclo.
5. Exibir no dashboard que a última coleta falhou, quando aplicável.

---

## 16. Validação dos candles

Validação, deduplicação e qualidade dos dados estão definidas na SPEC `0003-market-data-quality.md`

---

## 17. Ordenação dos candles

O provider pode retornar candles fora de ordem.

A aplicação deverá normalizar a ordenação antes de persistir ou analisar.

Ordem padrão:

```text
OpenTime ascendente
```

---

## 18. Histórico inicial

Ao iniciar a aplicação pela primeira vez, o sistema poderá precisar popular o histórico inicial.

Para o MVP:

```text
Buscar candles suficientes para a janela de análise configurada.
```

Exemplo:

```text
RegressionWindowCandles = 120
EMA máxima = 50
Mínimo recomendado = 120 candles
```

Para respeitar a ideia de histórico de 30 dias, a aplicação poderá evoluir para uma carga inicial maior.

## 18.1 Estratégia recomendada

1. No primeiro start, buscar o máximo disponível dentro de 30 dias.
2. Se o provider limitar a quantidade de candles, buscar em blocos.
3. Persistir com deduplicação.
4. Permitir que a aplicação funcione parcialmente se ainda não houver candles suficientes.

---

## 19. Contratos auxiliares sugeridos

## 19.1 IMarketDataProviderFactory

```csharp
public interface IMarketDataProviderFactory
{
    IMarketDataProvider Create();
}
```

Responsabilidade:

```text
Criar o provider correto com base na configuração.
```

## 19.2 IMarketCandleNormalizer

```csharp
public interface IMarketCandleNormalizer
{
    IReadOnlyCollection<MarketCandle> Normalize(
        IReadOnlyCollection<MarketCandle> candles,
        string internalSymbol,
        string internalTimeframe);
}
```

Responsabilidade:

```text
Padronizar símbolo, timeframe, data/hora e ordenação.
```

## 19.3 ICandleValidator 

```csharp
public interface ICandleValidator
{
    CandleValidationResult Validate(MarketCandle candle);
}
```

Responsabilidade:

```text
Validar se o candle é utilizável antes da persistência.
```

---

## 20. Fluxo de coleta

```text
Background Worker
↓
Lê provider configurado
↓
Lê símbolos habilitados
↓
Define intervalo de busca
↓
Chama IMarketDataProvider
↓
Normaliza candles
↓
Valida candles
↓
Remove candles inválidos
↓
Persiste candles válidos com AddOrUpdateAsync
↓
Registra status da coleta
↓
Aguarda próximo ciclo
```

---

## 21. Critérios de aceite

## Cenário 1 — Usar provider configurado

Dado que o `ProviderType` está configurado como `Mock`
Quando a aplicação iniciar
Então deverá utilizar `MockMarketDataProvider` para obter candles.

---

## Cenário 2 — Buscar candles do símbolo configurado

Dado que o símbolo `XAUUSD` está habilitado
Quando o worker executar a coleta
Então deverá solicitar candles de `XAUUSD` ao provider configurado.

---

## Cenário 3 — Normalizar candles

Dado que o provider retorna candles em formato suportado
Quando a aplicação processar a resposta
Então os candles deverão estar no padrão interno `MarketCandle`.

---

## Cenário 4 — Persistir sem duplicar

Dado que um candle já existe para o mesmo símbolo, timeframe e horário
Quando a coleta retornar o mesmo candle novamente
Então a aplicação não deverá criar duplicidade.

---

## Cenário 5 — Ignorar candle inválido

Dado que o provider retorna um candle com preço inválido
Quando a aplicação validar os candles
Então o candle inválido não deverá ser persistido.

---

## Cenário 6 — Falha do provider

Dado que o provider esteja indisponível
Quando o worker executar a coleta
Então a aplicação deverá registrar o erro e continuar funcionando.

---

## Cenário 7 — Ignorar candle em formação

Dado que `UseOnlyClosedCandles` está habilitado
Quando o provider retornar o candle ainda em formação
Então a aplicação deverá ignorá-lo para análise.

---

## Cenário 8 — Provider retorna candles fora de ordem

Dado que o provider retorna candles fora de ordem
Quando a aplicação normalizar a resposta
Então os candles deverão ser ordenados por `OpenTime` ascendente.

---

## 22. Testes mínimos esperados

## 22.1 Testes do MockMarketDataProvider

Validar:

1. Retorno de candles para mercado estável.
2. Retorno de candles para tendência de alta.
3. Retorno de candles para tendência de baixa.
4. Retorno de candles para rompimento de baixa.
5. Retorno de candles para rompimento de alta.
6. Simulação de resposta vazia.
7. Simulação de falha.

## 22.2 Testes de normalização

Validar:

1. Normalização de símbolo.
2. Normalização de timeframe.
3. Conversão de timezone para UTC.
4. Ordenação dos candles.

## 22.3 Testes de validação

Validar:

1. Candle válido.
2. Candle sem símbolo.
3. Candle sem timeframe.
4. Candle com preço zero.
5. Candle com máxima menor que mínima.
6. Candle com fechamento fora da máxima/mínima.

## 22.4 Testes de deduplicação

Validar:

1. Inserção de candle novo.
2. Atualização de candle existente.
3. Não duplicar por `Symbol + Timeframe + OpenTime`.

## 22.5 Testes de falha

Validar:

1. Timeout do provider.
2. Erro de autenticação.
3. Provider indisponível.
4. Resposta vazia.
5. Dados inválidos.

---

## 23. Riscos conhecidos

1. A FotMarket pode não possuir API pública adequada. (FotMarket é o nome do provider de dados de mercado considerado para a fase real)
2. Dados de provider gratuito podem ter atraso.
3. Ouro/XAUUSD pode ter símbolo diferente entre plataformas.
4. Timezone errado compromete a análise.
5. Candle em formação pode gerar falso rompimento.
6. Falhas intermitentes podem criar buracos no histórico.
7. Providers diferentes podem retornar preços ligeiramente diferentes.

---

## 24. Decisões importantes

1. A aplicação dependerá de `IMarketDataProvider`, não de provider concreto.
2. O MVP deverá começar com `MockMarketDataProvider`.
3. A aplicação deverá usar candles fechados para análise.
4. O candle será único por `Symbol + Timeframe + OpenTime`.
5. Horários deverão ser normalizados para UTC.
6. Provider real será plugado depois que houver fonte confiável.
7. A SPEC 0002 não define regra de análise técnica.

---

## 25. Resultado esperado

Ao final desta SPEC, a aplicação deverá possuir uma camada de dados de mercado capaz de:

1. selecionar provider por configuração;
2. buscar candles de mercado;
3. normalizar candles;
4. validar candles;
5. ignorar dados inválidos;
6. evitar duplicidade;
7. persistir candles válidos;
8. continuar funcionando em caso de falha de provider;
9. permitir desenvolvimento inicial com provider mockado.

Essa camada será a base para a análise técnica definida na SPEC 0001.
