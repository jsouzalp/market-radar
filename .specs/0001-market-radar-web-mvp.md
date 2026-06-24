# SPEC 0001 — Market Radar Web MVP

## 1. Visão geral

Criar uma aplicação web simples para monitoramento de ativos financeiros, inicialmente focada em **XAUUSD/Ouro**, com coleta periódica de candles, armazenamento histórico, cálculo de tendência, médias móveis e geração de alertas visuais/sonoros.

A ferramenta deverá atuar como um **radar técnico de oportunidades e riscos**, sem executar ordens de compra ou venda.

O objetivo principal é alertar o usuário quando houver sinais relevantes de rompimento de tendência, especialmente quando o preço romper uma linha de tendência calculada estatisticamente e esse rompimento for confirmado por candles consecutivos e médias móveis.

---

## 2. Objetivo do MVP

O MVP deverá permitir:

1. Monitorar um ativo configurável, inicialmente `XAUUSD`.
2. Coletar candles de 1 minuto.
3. Armazenar histórico dos últimos 30 dias.
4. Calcular linha de tendência por regressão linear.
5. Calcular EMA 9, EMA 21 e EMA 50.
6. Detectar rompimento de tendência.
7. Calcular score do alerta.
8. Exibir dashboard web simples.
9. Emitir alerta visual e sonoro.
10. Registrar histórico dos alertas.

---

## 3. Fora do escopo do MVP

A primeira versão não deverá:

1. Executar ordens de compra ou venda.
2. Integrar com conta real de corretora para operação.
3. Usar IA como decisor principal.
4. Fazer backtest completo.
5. Fazer envio por WhatsApp, Telegram ou e-mail.
6. Ter frontend separado em React ou Angular.
7. Fazer análise por imagem do gráfico.
8. Usar múltiplos ativos simultâneos como requisito obrigatório.
9. Fazer recomendação final de investimento.

---

## 4. Arquitetura recomendada

A aplicação deverá ser criada em **.NET 10**.

Estrutura recomendada:

```text
MarketRadar.sln
│
├── src
│   ├── MarketRadar.Web
│   ├── MarketRadar.Application
│   ├── MarketRadar.Domain
│   └── MarketRadar.Infrastructure
│
└── tests
    ├── MarketRadar.Domain.Tests
    └── MarketRadar.Application.Tests
```

---

## 5. Tipo de aplicação web

A aplicação deverá ser uma web app simples.

Recomendação para o MVP:

```text
Blazor Server
```

Justificativa:

1. Permite desenvolvimento full .NET.
2. Evita criar API + frontend separado no início.
3. Facilita dashboard com atualização em tempo real.
4. Permite evolução futura para API + frontend separado.
5. Combina bem com background worker e serviços internos.

---

## 6. Responsabilidades dos projetos

## 6.1 MarketRadar.Domain

Contém regras puras e modelos centrais.

Responsabilidades:

* entidades;
* enums;
* value objects;
* cálculo de regressão linear;
* cálculo de EMA;
* modelos de análise;
* regras de rompimento de tendência.

Não deve depender de:

* banco de dados;
* tela;
* API externa;
* arquivos de configuração;
* serviços de infraestrutura.

---

## 6.2 MarketRadar.Application

Contém orquestração dos casos de uso.

Responsabilidades:

* coordenar análise de mercado;
* acionar serviços de domínio;
* definir contratos de providers;
* definir contratos de repositórios;
* coordenar geração de alertas;
* preparar dados para dashboard.

---

## 6.3 MarketRadar.Infrastructure

Contém implementações externas.

Responsabilidades:

* acesso a banco de dados;
* implementação de repositórios;
* provider de mercado;
* provider mockado;
* persistência de candles;
* persistência de alertas;
* background worker;
* serviços de notificação.

---

## 6.4 MarketRadar.Web

Contém a interface web.

Responsabilidades:

* dashboard;
* componentes visuais;
* gráfico;
* cards de status;
* lista de alertas;
* alerta visual;
* alerta sonoro;
* atualização em tempo real.

Não deve conter regra de análise técnica.

---

## 7. Conceitos principais

## 7.1 Candle

A aplicação deverá trabalhar com candles, e não apenas com preço isolado.

Cada candle representa o comportamento do ativo em determinado intervalo.

No MVP, será usado o timeframe:

```text
M1 — candle de 1 minuto
```

Campos básicos:

* símbolo;
* timeframe;
* horário de abertura;
* preço de abertura;
* máxima;
* mínima;
* fechamento;
* volume, quando disponível.

---

## 7.2 Linha de tendência por regressão linear

A linha de tendência inicial será calculada usando regressão linear sobre os preços de fechamento dos últimos N candles.

A fórmula conceitual da reta será:

```text
y = a + b*x
```

Onde:

* `x` representa a posição temporal do candle;
* `y` representa o preço de fechamento;
* `a` é o intercepto;
* `b` é a inclinação da tendência.

Interpretação:

* `b > 0`: tendência de alta;
* `b < 0`: tendência de baixa;
* `b próximo de 0`: tendência lateral ou indefinida.

---

## 7.3 Médias móveis exponenciais

A aplicação deverá calcular EMAs sobre o preço de fechamento.

Períodos iniciais:

```text
EMA 9
EMA 21
EMA 50
```

Uso previsto:

* EMA 9: força mais curta;
* EMA 21: confirmação de movimento;
* EMA 50: referência de tendência mais ampla.

---

## 7.4 Rompimento de tendência

O rompimento não deverá ser detectado por uma única cotação.

A aplicação deverá exigir confirmação por candles consecutivos.

Exemplo:

```text
Rompimento de baixa:
- tendência anterior era de alta;
- últimos K candles fecharam abaixo da linha de regressão;
- distância em relação à linha passou de um limite estatístico;
- preço fechou abaixo da EMA 21;
- score final atingiu o mínimo configurado.
```

---

## 8. Banco de dados

O banco poderá ser SQL Server ou PostgreSQL.

Para o MVP, a escolha poderá seguir a preferência do projeto.

---

## 8.1 Tabela MarketCandles

```sql
CREATE TABLE MarketCandles (
    Id BIGINT IDENTITY PRIMARY KEY,
    Symbol NVARCHAR(30) NOT NULL,
    Timeframe NVARCHAR(10) NOT NULL,
    OpenTime DATETIME2 NOT NULL,
    OpenPrice DECIMAL(18, 8) NOT NULL,
    HighPrice DECIMAL(18, 8) NOT NULL,
    LowPrice DECIMAL(18, 8) NOT NULL,
    ClosePrice DECIMAL(18, 8) NOT NULL,
    Volume DECIMAL(18, 8) NULL,
    CreatedAt DATETIME2 NOT NULL
);
```

Índice único:

```sql
CREATE UNIQUE INDEX UX_MarketCandles_Symbol_Timeframe_OpenTime
ON MarketCandles(Symbol, Timeframe, OpenTime);
```

Índice de consulta:

```sql
CREATE INDEX IX_MarketCandles_Symbol_Timeframe_OpenTime
ON MarketCandles(Symbol, Timeframe, OpenTime);
```

Observação:

O índice único é importante para impedir duplicidade de candle, especialmente quando o worker buscar dados sobrepostos.

---

## 8.2 Tabela MarketAlerts

```sql
CREATE TABLE MarketAlerts (
    Id BIGINT IDENTITY PRIMARY KEY,
    Symbol NVARCHAR(30) NOT NULL,
    Timeframe NVARCHAR(10) NOT NULL,
    AlertType NVARCHAR(50) NOT NULL,
    Severity NVARCHAR(30) NOT NULL,
    Score DECIMAL(5, 2) NOT NULL,
    Message NVARCHAR(1000) NOT NULL,
    CreatedAt DATETIME2 NOT NULL
);
```

---

## 9. Configuração

A aplicação deverá ler configurações a partir do `appsettings.json`.

Exemplo:

```json
{
  "MarketMonitor": {
    "PollingIntervalSeconds": 60,
    "HistoryDays": 30,
    "Symbols": [
      {
        "Code": "XAUUSD",
        "Enabled": true,
        "Timeframe": "M1"
      }
    ]
  },
  "TrendAnalysis": {
    "Enabled": true,
    "RegressionWindowCandles": 120,
    "RequiredBreakCandles": 3,
    "DeviationMultiplier": 1.5,
    "MinimumSlope": 0.001,
    "MinimumBreakScore": 70
  },
  "MovingAverages": {
    "Enabled": true,
    "Periods": [9, 21, 50]
  },
  "Alerts": {
    "EnableSound": true,
    "EnableVisualHighlight": true,
    "EnableBrowserNotification": false
  }
}
```

---

## 10. Funcionalidades do MVP

## 10.1 Coleta de candles

A aplicação deverá possuir um background worker responsável por buscar candles recentes.

Regras:

1. Buscar candles periodicamente.
2. Intervalo configurável.
3. Persistir candles no banco.
4. Evitar duplicidade.
5. Continuar executando mesmo se uma coleta falhar.
6. Registrar falhas em log.
7. Suportar provider mockado para testes.

---

## 10.2 Histórico de 30 dias

A aplicação deverá manter no banco o histórico dos últimos 30 dias para cada ativo monitorado.

No MVP, poderá ser implementado sem rotina automática de expurgo.

Evolução futura:

```text
Criar rotina para remover candles antigos conforme configuração de retenção.
```

---

## 10.3 Cálculo de regressão linear

A aplicação deverá buscar os últimos N candles configurados e calcular a regressão linear.

Entrada:

* símbolo;
* timeframe;
* últimos N candles;
* preço usado: `ClosePrice`.

Saída esperada:

* slope;
* intercept;
* preço atual da linha;
* preço de fechamento atual;
* distância do preço até a linha;
* desvio padrão dos resíduos;
* direção da tendência.

---

## 10.4 Cálculo de EMAs

A aplicação deverá calcular as EMAs configuradas.

Períodos iniciais:

```text
9, 21, 50
```

Para cada EMA, o resultado deverá informar:

* período;
* valor atual;
* valor anterior;
* direção;
* se o preço atual está acima ou abaixo da média.

---

## 10.5 Análise de rompimento

A aplicação deverá analisar rompimentos de alta e baixa.

### Rompimento de baixa

Condições iniciais:

1. A regressão dos últimos N candles indica tendência de alta.
2. Os últimos K candles fecharam abaixo da linha de regressão.
3. A distância média dos candles rompidos é maior que o desvio padrão dos resíduos multiplicado pelo fator configurado.
4. O preço atual fechou abaixo da EMA 21.
5. O score final é maior ou igual ao mínimo configurado.

---

### Rompimento de alta

Condições iniciais:

1. A regressão dos últimos N candles indica tendência de baixa.
2. Os últimos K candles fecharam acima da linha de regressão.
3. A distância média dos candles rompidos é maior que o desvio padrão dos resíduos multiplicado pelo fator configurado.
4. O preço atual fechou acima da EMA 21.
5. O score final é maior ou igual ao mínimo configurado.

---

## 10.6 Score do alerta

A aplicação deverá calcular um score de 0 a 100.

Sugestão inicial:

```text
+25 pontos: rompeu a linha de regressão
+20 pontos: manteve rompimento por K candles seguidos
+20 pontos: distância maior que o limite estatístico
+15 pontos: preço rompeu a EMA 21
+10 pontos: EMA 9 virou contra a tendência anterior
+10 pontos: rompeu máxima ou mínima recente
```

Classificação:

```text
0-49   = ignorar
50-69  = atenção
70-84  = alerta
85-100 = alerta forte
```

---

## 10.7 Alertas

A aplicação deverá gerar alerta quando o score atingir o mínimo configurado.

Tipos iniciais:

```csharp
public enum AlertType
{
    TrendBreakDown = 1,
    TrendBreakUp = 2,
    AbruptDrop = 3,
    AbruptRise = 4,
    PossibleFalseBreakout = 5
}
```

Severidade:

```csharp
public enum AlertSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3
}
```

O alerta deverá conter:

* símbolo;
* timeframe;
* tipo;
* severidade;
* score;
* mensagem;
* data/hora.

---

## 10.8 Dashboard web

A tela principal deverá exibir:

1. Ativo monitorado.
2. Preço atual.
3. Variação do último candle.
4. Direção da regressão.
5. Inclinação da regressão.
6. Valor da EMA 9.
7. Valor da EMA 21.
8. Valor da EMA 50.
9. Último alerta.
10. Score do último alerta.
11. Horário da última atualização.
12. Lista dos alertas recentes.

---

## 10.9 Gráfico

O dashboard deverá exibir um gráfico com:

* preço de fechamento;
* linha de regressão;
* EMA 9;
* EMA 21;
* EMA 50;
* marcação dos alertas.

Para o MVP, o gráfico poderá ser de linha.

Candlestick completo será evolução futura.

Biblioteca recomendada:

```text
TradingView Lightweight Charts
```

Justificativa:

* apropriada para dados financeiros;
* leve;
* suporta candlestick;
* suporta linhas auxiliares;
* permite evolução natural do dashboard.

---

## 10.10 Alerta visual e sonoro

Quando um alerta for gerado:

1. O dashboard deverá destacar o evento.
2. A linha do alerta deverá receber destaque visual.
3. Um som deverá ser emitido se `EnableSound` estiver habilitado.
4. O alerta deverá ser salvo no banco.
5. O alerta deverá aparecer no histórico.

---

## 11. Serviços e contratos

## 11.1 IMarketDataProvider

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

Implementações previstas:

* MockMarketDataProvider;
* FotMarketProvider, se houver API disponível;
* MetaTraderProvider;
* outro provider de mercado público.

---

## 11.2 ICandleRepository

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

---

## 11.3 ITrendLineService

```csharp
public interface ITrendLineService
{
    TrendLineResult Calculate(IReadOnlyList<MarketCandle> candles, TrendAnalysisSettings settings);
}
```

---

## 11.4 IMovingAverageService

```csharp
public interface IMovingAverageService
{
    IReadOnlyCollection<MovingAverageResult> CalculateEma(
        IReadOnlyList<MarketCandle> candles,
        IReadOnlyCollection<int> periods);
}
```

---

## 11.5 ITrendBreakAnalysisService

```csharp
public interface ITrendBreakAnalysisService
{
    TrendBreakAnalysisResult Analyze(
        IReadOnlyList<MarketCandle> candles,
        TrendAnalysisSettings trendSettings,
        MovingAverageSettings movingAverageSettings);
}
```

---

## 11.6 IAlertRepository

```csharp
public interface IAlertRepository
{
    Task AddAsync(
        MarketAlert alert,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<MarketAlert>> GetRecentAsync(
        string symbol,
        int count,
        CancellationToken cancellationToken);
}
```

---

## 11.7 IAlertDispatcher

```csharp
public interface IAlertDispatcher
{
    Task DispatchAsync(
        MarketAlert alert,
        CancellationToken cancellationToken);
}
```

---

## 12. Modelos principais

## 12.1 MarketCandle

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

---

## 12.2 TrendLineResult

```csharp
public class TrendLineResult
{
    public decimal Slope { get; set; }
    public decimal Intercept { get; set; }
    public decimal CurrentTrendPrice { get; set; }
    public decimal CurrentClosePrice { get; set; }
    public decimal DistanceFromTrendLine { get; set; }
    public decimal ResidualStandardDeviation { get; set; }
    public TrendDirection Direction { get; set; }
}
```

---

## 12.3 TrendDirection

```csharp
public enum TrendDirection
{
    Neutral = 0,
    Up = 1,
    Down = 2
}
```

---

## 12.4 MovingAverageResult

```csharp
public class MovingAverageResult
{
    public int Period { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal PreviousValue { get; set; }
    public MovingAverageDirection Direction { get; set; }
    public bool CurrentPriceIsAbove { get; set; }
}
```

---

## 12.5 MovingAverageDirection

```csharp
public enum MovingAverageDirection
{
    Neutral = 0,
    Up = 1,
    Down = 2
}
```

---

## 12.6 TrendBreakAnalysisResult

```csharp
public class TrendBreakAnalysisResult
{
    public string Symbol { get; set; }
    public string Timeframe { get; set; }
    public TrendAnalysisStatus Status { get; set; }
    public bool HasAlert { get; set; }
    public AlertType? AlertType { get; set; }
    public AlertSeverity? Severity { get; set; }
    public decimal Score { get; set; }
    public TrendLineResult TrendLine { get; set; }
    public IReadOnlyCollection<MovingAverageResult> MovingAverages { get; set; }
    public string Message { get; set; }
}
```

---

## 12.7 MarketAlert

```csharp
public class MarketAlert
{
    public long Id { get; set; }
    public string Symbol { get; set; }
    public string Timeframe { get; set; }
    public AlertType AlertType { get; set; }
    public AlertSeverity Severity { get; set; }
    public decimal Score { get; set; }
    public string Message { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

## 13. Fluxo principal

```text
Background Worker
↓
Lê configurações
↓
Obtém símbolos habilitados
↓
Busca candles recentes no provider
↓
Persiste candles no banco
↓
Busca últimos N candles do banco
↓
Calcula regressão linear
↓
Calcula EMA 9, EMA 21 e EMA 50
↓
Analisa rompimento de tendência
↓
Calcula score
↓
Se houver alerta:
    salva alerta no banco
    atualiza dashboard
    dispara alerta visual/sonoro
↓
Aguarda próximo ciclo
```

---

## 14. Critérios de aceite

## Cenário 1 — Coleta de candles

Dado que o símbolo `XAUUSD` está habilitado
Quando o background worker executar
Então a aplicação deverá buscar candles recentes e persistir no banco.

---

## Cenário 2 — Evitar duplicidade

Dado que um candle já existe para o mesmo símbolo, timeframe e horário
Quando o worker tentar inserir novamente
Então a aplicação não deverá duplicar o registro.

---

## Cenário 3 — Cálculo de regressão linear

Dado que existem candles suficientes no banco
Quando a análise for executada
Então a aplicação deverá calcular slope, intercept, preço atual da linha, distância e desvio padrão dos resíduos.

---

## Cenário 4 — Cálculo de EMA

Dado que existem candles suficientes no banco
Quando a análise for executada
Então a aplicação deverá calcular EMA 9, EMA 21 e EMA 50.

---

## Cenário 5 — Rompimento de baixa

Dado que a tendência calculada é de alta
E os últimos candles fecharam abaixo da linha de regressão
E o preço fechou abaixo da EMA 21
E o score atingiu o mínimo configurado
Quando a análise for concluída
Então a aplicação deverá gerar alerta de rompimento de baixa.

---

## Cenário 6 — Rompimento de alta

Dado que a tendência calculada é de baixa
E os últimos candles fecharam acima da linha de regressão
E o preço fechou acima da EMA 21
E o score atingiu o mínimo configurado
Quando a análise for concluída
Então a aplicação deverá gerar alerta de rompimento de alta.

---

## Cenário 7 — Dashboard

Dado que existem candles e análises disponíveis
Quando o usuário abrir o dashboard
Então deverá visualizar preço atual, linha de tendência, EMAs e alertas recentes.

---

## Cenário 8 — Alerta sonoro

Dado que um alerta foi gerado
E a configuração de som está habilitada
Quando o dashboard estiver aberto
Então a aplicação deverá emitir um alerta sonoro.

---

## Cenário 9 — Falha no provider

Dado que o provider de mercado esteja indisponível
Quando o worker tentar buscar candles
Então a aplicação deverá registrar log de erro e continuar funcionando.

---

## 15. Requisitos não funcionais

1. A aplicação deve ser resiliente a falhas temporárias.
2. A falha de coleta não deve derrubar a aplicação.
3. As regras de análise devem ser testáveis sem banco e sem provider externo.
4. O dashboard não deve conter regra de negócio.
5. A coleta deve ser configurável.
6. O histórico deve evitar duplicidade.
7. O sistema deve permitir provider mockado.
8. A solução deve permitir evolução para múltiplos símbolos.
9. A solução deve permitir evolução para IA.
10. A solução deve registrar logs de erro e alertas.

---

## 16. Testes mínimos esperados

## 16.1 Testes de regressão linear

Validar:

1. Tendência de alta.
2. Tendência de baixa.
3. Tendência neutra.
4. Cálculo de slope.
5. Cálculo de intercept.
6. Cálculo de preço atual da linha.
7. Cálculo de desvio padrão dos resíduos.

---

## 16.2 Testes de EMA

Validar:

1. EMA 9.
2. EMA 21.
3. EMA 50.
4. Direção da EMA.
5. Comparação entre preço atual e EMA.

---

## 16.3 Testes de rompimento

Validar:

1. Rompimento de baixa válido.
2. Rompimento de alta válido.
3. Falso rompimento ignorado.
4. Rompimento sem candles suficientes ignorado.
5. Rompimento sem confirmação por K candles ignorado.
6. Score abaixo do mínimo não gera alerta.

---

## 16.4 Testes de repositório

Validar:

1. Inserção de candle.
2. Prevenção de duplicidade.
3. Busca dos últimos N candles.
4. Busca ordenada por horário.

---

## 17. Evoluções futuras

1. Candlestick real no gráfico.
2. Backtest.
3. Paper trading.
4. Integração com IA para explicar alertas.
5. Envio de alerta por Telegram.
6. Envio de alerta por WhatsApp.
7. Envio de alerta por e-mail.
8. Configuração dos ativos via tela.
9. Suporte a múltiplos timeframes.
10. Suporte a múltiplos ativos.
11. Detecção de suporte e resistência.
12. Linha de tendência por fundos e topos.
13. Ranking de assertividade dos alertas.
14. Filtro por horário de mercado.
15. Filtro por notícias de alto impacto.
16. Comparação entre diferentes janelas de tendência.
17. Relatório diário de alertas.
18. Dashboard de performance dos alertas.

---

## 18. Decisões importantes

1. O MVP usará regressão linear como primeira forma de linha de tendência.
2. O MVP usará EMA 9, EMA 21 e EMA 50 como confirmação.
3. O MVP armazenará candles M1.
4. O MVP terá dashboard web simples.
5. O MVP não executará ordens.
6. O MVP não dependerá de IA para funcionar.
7. A IA será tratada como evolução para explicação dos alertas.
8. A primeira versão priorizará confiabilidade da análise em vez de sofisticação visual.

---

## 19. Risco conhecido

A ferramenta não garante acerto de mercado.

Rompimento de tendência pode gerar falso positivo, especialmente em momentos de:

* baixa liquidez;
* spread elevado;
* notícia econômica;
* abertura de mercado;
* movimentos abruptos;
* lateralização.

Por isso, todo alerta deve ser tratado como sinal de atenção, não como ordem automática de compra ou venda.

---

## 20. Resultado esperado

Ao final da SPEC 0001, a aplicação deverá entregar um radar funcional capaz de:

1. coletar candles M1;
2. armazenar histórico;
3. calcular linha de tendência;
4. calcular EMAs;
5. detectar rompimentos relevantes;
6. gerar alertas;
7. exibir tudo em dashboard web simples.

O sistema deverá estar preparado para evoluir para IA, backtest, múltiplos ativos e integrações de notificação.
