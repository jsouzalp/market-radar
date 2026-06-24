# SPEC 0004 — Trend Analysis Engine

## 1. Objetivo

Definir o motor de análise técnica responsável por calcular tendência, médias móveis, rompimentos, falso rompimento e score de alerta a partir de candles já validados.

Esta SPEC complementa:

```text
0001-market-radar-web-mvp.md
0002-market-data-provider.md
0003-market-data-quality.md
```

A SPEC 0004 não trata de coleta de dados, dashboard ou integração com corretora. Ela define exclusivamente como a aplicação deverá transformar candles válidos em análise técnica.

---

## 2. Escopo

Esta SPEC cobre:

1. Cálculo de regressão linear.
2. Cálculo de linha de tendência.
3. Cálculo de distância entre preço e linha.
4. Cálculo de resíduos.
5. Cálculo de desvio padrão dos resíduos.
6. Cálculo de EMA.
7. Detecção de rompimento de tendência.
8. Detecção de possível falso rompimento.
9. Cálculo de score técnico.
10. Resultado consolidado da análise.

---

## 3. Fora do escopo

Esta SPEC não define:

1. Coleta de candles.
2. Validação de qualidade dos candles.
3. Persistência em banco.
4. Dashboard.
5. Alertas visuais ou sonoros.
6. Envio por Telegram, WhatsApp ou e-mail.
7. IA para explicação.
8. Execução de ordens.

---

## 4. Premissas

O motor de análise deverá receber apenas candles:

1. válidos;
2. fechados;
3. ordenados por horário;
4. normalizados;
5. em UTC;
6. com preços em `decimal`.

Essas garantias vêm da SPEC 0003.

---

## 5. Entrada principal

O motor deverá receber uma lista de candles. A estrutura de Marketcandle foi definida na SPEC `0002-market-data-provider.md`, seção 7  
Regra:  
```text
A lista deve estar ordenada por OpenTime em ordem crescente.
```

---

## 6. Configuração da análise

Adicionar ao `appsettings.json`:

```json
{
  "TrendAnalysis": {
    "Enabled": true,
    "RegressionWindowCandles": 120,
    "RequiredBreakCandles": 3,
    "DeviationMultiplier": 1.5,
    "MinimumSlope": 0.001,
    "MinimumBreakScore": 70,
    "RecentHighLowWindowCandles": 20,
    "FalseBreakoutLookbackCandles": 5
  },
  "MovingAverages": {
    "Enabled": true,
    "Periods": [9, 21, 50]
  }
}
```

---

## 7. Janela de análise

A análise deverá considerar os últimos `RegressionWindowCandles`.

Exemplo:

```text
RegressionWindowCandles = 120
Timeframe = M1
Janela analisada = últimos 120 minutos
```

Se não houver candles suficientes, a análise não deve ser executada.

Resultado esperado:

```text
Status: InsufficientData
```

---

## 8. Regressão linear

A linha de tendência inicial deverá ser calculada por regressão linear simples usando o `ClosePrice`.

Cada candle será representado como:

```text
x = índice do candle na janela
y = ClosePrice
```

Exemplo:

```text
Candle 1  => x = 1, y = 2330.15
Candle 2  => x = 2, y = 2330.80
Candle 3  => x = 3, y = 2331.20
```

A fórmula conceitual:

```text
y = a + b*x
```

Onde:

1. `a` = intercepto;
2. `b` = inclinação;
3. `x` = posição temporal;
4. `y` = preço estimado.

---

## 9. Interpretação do slope

O slope representa a direção da tendência.

Regras:

```text
Se slope > MinimumSlope      => tendência de alta
Se slope < -MinimumSlope     => tendência de baixa
Caso contrário               => tendência neutra
```

Exemplo:

```text
MinimumSlope = 0.001
Slope = 0.012  => Up
Slope = -0.015 => Down
Slope = 0.0004 => Neutral
```

---

## 10. Resultado da linha de tendência

O cálculo da regressão deverá retornar:

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

## 11. Resíduos

Para cada candle da janela:

```text
trendPrice = intercept + slope * x
residual = closePrice - trendPrice
```

O resíduo mede quanto o preço real ficou acima ou abaixo da linha estimada.

---

## 12. Desvio padrão dos resíduos

O motor deverá calcular o desvio padrão dos resíduos.

Uso:

```text
O desvio padrão dos resíduos será usado para evitar alertas por pequenas oscilações.
```

Exemplo:

```text
TrendPrice = 2350.00
ClosePrice = 2349.85
Distância = -0.15
ResidualStandardDeviation = 1.20
```

Nesse caso, a distância é pequena e não deve gerar rompimento relevante.

---

## 13. Limite estatístico de rompimento

O limite mínimo de rompimento será:

```text
breakThreshold = ResidualStandardDeviation * DeviationMultiplier
```

Exemplo:

```text
ResidualStandardDeviation = 1.20
DeviationMultiplier = 1.5

breakThreshold = 1.80
```

Para rompimento de baixa:

```text
ClosePrice precisa estar abaixo da linha por mais de 1.80
```

Para rompimento de alta:

```text
ClosePrice precisa estar acima da linha por mais de 1.80
```

---

## 14. Cálculo de EMA

O motor deverá calcular médias móveis exponenciais usando `ClosePrice`.

Períodos iniciais:

```text
EMA 9
EMA 21
EMA 50
```

Fórmula conceitual:

```text
Multiplier = 2 / (Period + 1)
EMA atual = (ClosePrice atual - EMA anterior) * Multiplier + EMA anterior
```

Para inicialização, a primeira EMA poderá usar SMA dos primeiros N candles.

---

## 15. Resultado da EMA

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

Direção:

```text
CurrentValue > PreviousValue => Up
CurrentValue < PreviousValue => Down
Caso contrário               => Neutral
```

---

## 16. Rompimento de baixa

Um rompimento de baixa poderá ser identificado quando:

1. A tendência calculada pela regressão for de alta.
2. Os últimos `RequiredBreakCandles` fecharem abaixo da linha de tendência.
3. A distância média desses candles para a linha for maior que `breakThreshold`.
4. O fechamento atual estiver abaixo da EMA 21.
5. O score final for maior ou igual a `MinimumBreakScore`.

Regra conceitual:

```text
TrendDirection == Up
AND Last K closes < trend line
AND AverageDistanceBelowLine > breakThreshold
AND CurrentClose < EMA21
AND Score >= MinimumBreakScore
```

---

## 17. Rompimento de alta

Um rompimento de alta poderá ser identificado quando:

1. A tendência calculada pela regressão for de baixa.
2. Os últimos `RequiredBreakCandles` fecharem acima da linha de tendência.
3. A distância média desses candles para a linha for maior que `breakThreshold`.
4. O fechamento atual estiver acima da EMA 21.
5. O score final for maior ou igual a `MinimumBreakScore`.

Regra conceitual:

```text
TrendDirection == Down
AND Last K closes > trend line
AND AverageDistanceAboveLine > breakThreshold
AND CurrentClose > EMA21
AND Score >= MinimumBreakScore
```

---

## 18. Tendência neutra

Se a tendência for neutra:

```text
TrendDirection == Neutral
```

A aplicação não deverá gerar alerta de rompimento de tendência.

Motivo:

```text
Não há tendência clara a ser rompida.
```

Poderá retornar:

```text
Status: NeutralTrend
```

---

## 19. Máxima e mínima recente

A análise deverá considerar a máxima e mínima dos últimos `RecentHighLowWindowCandles`.

Uso:

1. confirmar rompimento;
2. aumentar score;
3. reduzir falso positivo.

Para rompimento de baixa:

```text
CurrentClose < menor LowPrice da janela recente
```

Para rompimento de alta:

```text
CurrentClose > maior HighPrice da janela recente
```

---

## 20. Score técnico

O score deverá variar de 0 a 100.

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
Observação: Pontos são acumulativos e cada critério é verificado de forma independente  
---

## 21. Severidade

A severidade deverá ser derivada do score.

```csharp
public enum AlertSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3
}
```

Mapeamento sugerido:

```text
0-49   => sem alerta
50-69  => Info
70-84  => Warning
85-100 => Critical
```

---

## 22. Falso rompimento

A primeira versão não tentará prever falso rompimento com perfeição.

Porém, deverá evitar alertas fracos com as seguintes proteções:

1. exigir K candles consecutivos;
2. exigir distância estatística mínima;
3. exigir confirmação pela EMA 21;
4. ignorar tendência neutra;
5. bloquear análise com dados obsoletos, conforme SPEC 0003.

---

## 23. Possível falso rompimento após alerta

Após um alerta, a aplicação poderá identificar possível falso rompimento se, nos próximos candles, o preço voltar para dentro da linha de tendência.

Regra futura, opcional no MVP:

```text
Se após alerta de baixa o preço voltar acima da linha em até N candles,
marcar alerta como PossibleFalseBreakout.
```

Essa regra não precisa bloquear o MVP, mas deve ficar prevista no modelo.

---

## 24. Resultado consolidado da análise

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

## 25. Status da análise

```csharp
public enum TrendAnalysisStatus
{
    Unknown = 0,
    Success = 1,
    InsufficientData = 2,
    NeutralTrend = 3,
    NoBreakout = 4,
    BreakoutDetected = 5,
    QualityBlocked = 6
}
```

---

## 26. Tipos de alerta

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

Para o MVP, os principais serão:

```text
TrendBreakDown
TrendBreakUp
```

---

## 27. Serviço principal

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

## 28. Serviços auxiliares

## 28.1 ITrendLineService

```csharp
public interface ITrendLineService
{
    TrendLineResult Calculate(
        IReadOnlyList<MarketCandle> candles,
        TrendAnalysisSettings settings);
}
```

---

## 28.2 IMovingAverageService

```csharp
public interface IMovingAverageService
{
    IReadOnlyCollection<MovingAverageResult> CalculateEma(
        IReadOnlyList<MarketCandle> candles,
        IReadOnlyCollection<int> periods);
}
```

---

## 28.3 ITrendScoreService

```csharp
public interface ITrendScoreService
{
    decimal CalculateScore(
        TrendLineResult trendLine,
        IReadOnlyCollection<MovingAverageResult> movingAverages,
        IReadOnlyList<MarketCandle> candles,
        TrendAnalysisSettings settings);
}
```

---

## 29. Mensagem técnica do resultado

O resultado deverá gerar uma mensagem curta e objetiva.

Exemplo:

```text
XAUUSD rompeu a tendência de alta. Os últimos 3 candles fecharam abaixo da linha de regressão e o preço atual está abaixo da EMA 21. Score: 78.
```

A mensagem não deve dizer:

```text
Compre agora
Venda agora
Operação garantida
Lucro provável
```

A mensagem deve ser tratada como alerta técnico, não recomendação final.

---

## 30. Fluxo da análise

```text
Receber candles válidos
↓
Verificar quantidade mínima
↓
Calcular regressão linear
↓
Calcular resíduos
↓
Calcular desvio padrão dos resíduos
↓
Determinar direção da tendência
↓
Calcular EMAs
↓
Verificar rompimento de baixa ou alta
↓
Calcular score
↓
Definir severidade
↓
Retornar resultado consolidado
```

---

## 31. Critérios de aceite

## Cenário 1 — Tendência de alta

Dado um conjunto de candles com fechamentos crescentes  
Quando a regressão for calculada  
Então a tendência deverá ser classificada como alta.

---

## Cenário 2 — Tendência de baixa

Dado um conjunto de candles com fechamentos decrescentes  
Quando a regressão for calculada  
Então a tendência deverá ser classificada como baixa.

---

## Cenário 3 — Tendência neutra

Dado um conjunto de candles sem direção clara  
Quando a regressão for calculada  
Então a tendência deverá ser classificada como neutra.

---

## Cenário 4 — Rompimento de baixa válido

Dado que a tendência anterior é de alta  
E os últimos candles fecharam abaixo da linha  
E o preço atual está abaixo da EMA 21  
Quando o score atingir o mínimo configurado  
Então o resultado deverá indicar `TrendBreakDown`.

---

## Cenário 5 — Rompimento de alta válido

Dado que a tendência anterior é de baixa  
E os últimos candles fecharam acima da linha  
E o preço atual está acima da EMA 21  
Quando o score atingir o mínimo configurado  
Então o resultado deverá indicar `TrendBreakUp`.

---

## Cenário 6 — Falso rompimento ignorado

Dado que apenas um candle rompeu a linha  
Quando `RequiredBreakCandles` for maior que 1  
Então a aplicação não deverá gerar alerta.

---

## Cenário 7 — Score insuficiente

Dado que houve rompimento parcial  
Quando o score ficar abaixo do mínimo  
Então a aplicação não deverá gerar alerta.

---

## Cenário 8 — Dados insuficientes

Dado que existem menos candles que a janela necessária  
Quando a análise for executada  
Então o status deverá ser `InsufficientData`.

---

## 32. Testes mínimos esperados

## 32.1 Regressão linear

Validar:

1. slope positivo;
2. slope negativo;
3. slope próximo de zero;
4. intercept;
5. preço atual da linha;
6. distância do preço até a linha;
7. desvio padrão dos resíduos.

---

## 32.2 EMA

Validar:

1. EMA 9;
2. EMA 21;
3. EMA 50;
4. direção da EMA;
5. preço acima da EMA;
6. preço abaixo da EMA.

---

## 32.3 Rompimento

Validar:

1. rompimento de baixa;
2. rompimento de alta;
3. rompimento com apenas um candle ignorado;
4. rompimento sem confirmação da EMA ignorado;
5. tendência neutra não gera rompimento;
6. score abaixo do mínimo não gera alerta.

---

## 33. Decisões importantes

1. A linha de tendência inicial será regressão linear.
2. A regressão usará `ClosePrice`.
3. A análise usará apenas candles fechados.
4. A EMA 21 será confirmação principal.
5. A EMA 9 será usada para força curta.
6. A EMA 50 será usada como referência visual e futura confirmação.
7. O score evita alertas binários fracos.
8. O motor não recomenda compra ou venda.
9. O motor apenas identifica eventos técnicos.

---

## 34. Resultado esperado

Ao final desta SPEC, a aplicação deverá possuir um motor de análise capaz de:

1. calcular tendência por regressão linear;
2. calcular EMAs;
3. medir distância do preço para a linha;
4. medir relevância estatística do rompimento;
5. detectar rompimentos de alta e baixa;
6. calcular score;
7. retornar análise técnica consolidada;
8. evitar alertas fracos ou baseados em ruído.
