# SPEC 0003 — Market Data Quality and Candle Validation

## 1. Objetivo

Definir as regras de qualidade, validação, normalização e segurança dos candles usados pela aplicação Market Radar.

Esta SPEC complementa a `0002-market-data-provider.md`.

Enquanto a SPEC 0002 define como obter candles a partir de providers de mercado, esta SPEC define como garantir que esses candles estejam consistentes antes de serem persistidos, exibidos ou usados em análises técnicas.

O objetivo principal é evitar que dados ruins, incompletos, duplicados ou em formação gerem falsos alertas de rompimento, médias móveis incorretas ou distorções no dashboard.

---

## 2. Escopo

Esta SPEC cobre:

1. Validação de candles.
2. Normalização de preços.
3. Uso obrigatório de candles fechados para análise.
4. Tratamento de candles em formação.
5. Timezone padrão.
6. Precisão decimal.
7. Backfill inicial.
8. Deduplicação.
9. Comportamento em caso de falha do provider.
10. Regras mínimas para liberar análise técnica.

---

## 3. Fora do escopo

Esta SPEC não define:

1. Como buscar candles em providers externos.
2. Como calcular regressão linear.
3. Como calcular EMAs.
4. Como gerar alertas técnicos.
5. Como desenhar gráficos no dashboard.
6. Como fazer backtest.
7. Como executar ordens.

Esses pontos pertencem a outras SPECs.

---

## 4. Decisão principal

A aplicação só deverá usar **candles fechados** para geração de alertas e análises técnicas.

Candles em formação poderão ser exibidos no dashboard como informação visual, mas não deverão ser usados para:

1. regressão linear;
2. EMA;
3. rompimento de tendência;
4. score de alerta;
5. persistência definitiva como candle fechado.

---

## 5. Candle fechado versus candle em formação

## 5.1 Candle fechado

Um candle é considerado fechado quando seu intervalo de tempo já terminou.

Exemplo para timeframe `M1`:

```text
Candle OpenTime: 10:35:00
Timeframe: M1
Fechamento esperado: 10:36:00
```

Esse candle só deve ser considerado fechado a partir de `10:36:00`.

---

## 5.2 Candle em formação

Um candle em formação é o candle correspondente ao intervalo atual ainda não finalizado.

Exemplo:

```text
Horário atual: 10:35:30
Candle atual: 10:35:00 até 10:35:59
```

Esse candle ainda pode mudar.

Por isso, ele não deve gerar alerta técnico.

---

## 5.3 Regra obrigatória

```text
Somente candles fechados podem ser usados para análise técnica e geração de alertas.
```

---

## 6. Timezone padrão

Todos os candles deverão ser armazenados internamente em UTC.

Regra:

```text
OpenTime deve ser persistido em UTC.
```

A interface web poderá converter o horário para o timezone local do usuário.

Motivo:

1. evitar duplicidade;
2. evitar erro de ordenação;
3. evitar inconsistência entre providers;
4. facilitar backtest futuro;
5. facilitar comparação histórica.

---

## 7. Precisão decimal

Todos os preços deverão ser tratados como `decimal`.

Regra:

```text
Modelos internos não devem usar double ou float para preços.
```

Campos afetados:

1. `OpenPrice`;
2. `HighPrice`;
3. `LowPrice`;
4. `ClosePrice`;
5. `Volume`, quando aplicável;
6. valores de EMA;
7. valores de regressão;
8. distâncias e variações.

O provider poderá receber valores como `double`, `string` ou outro formato externo, mas deverá converter para `decimal` antes de retornar o modelo interno.

---

## 8. Regras de qualidade do candle

Antes de persistir ou usar um candle para análise, a aplicação deverá validar:

1. `Symbol` não pode ser vazio.
2. `Timeframe` não pode ser vazio.
3. `OpenTime` deve estar em UTC.
4. `OpenTime` não pode ser futuro.
5. `OpenPrice` deve ser maior que zero.
6. `HighPrice` deve ser maior que zero.
7. `LowPrice` deve ser maior que zero.
8. `ClosePrice` deve ser maior que zero.
9. `HighPrice` deve ser maior ou igual a `LowPrice`.
10. `OpenPrice` deve estar entre `LowPrice` e `HighPrice`.
11. `ClosePrice` deve estar entre `LowPrice` e `HighPrice`.
12. `Volume`, quando informado, não pode ser negativo.

---

## 9. Resultado da validação

A validação do candle deverá retornar um resultado explícito.

Exemplo conceitual:

```csharp
public class CandleValidationResult
{
    public bool IsValid { get; set; }
    public IReadOnlyCollection<string> Errors { get; set; }
}
```

A aplicação não deve lançar exceção para todo candle inválido individualmente.

Candles inválidos devem ser:

1. rejeitados;
2. registrados em log;
3. ignorados na análise;
4. não persistidos como candle válido.

---

## 10. Normalização de símbolo

Símbolos vindos de providers externos podem ter formatos diferentes.

Exemplos:

```text
XAUUSD
XAU/USD
GOLD
Gold
```

A aplicação deverá trabalhar internamente com um símbolo normalizado.

Para o MVP:

```text
XAUUSD
```

Deverá existir uma camada de normalização.

Exemplo conceitual:

```csharp
public interface ISymbolNormalizer
{
    string Normalize(string providerSymbol);
}
```

---

## 11. Normalização de timeframe

Timeframes externos também podem variar.

Exemplos:

```text
M1
1m
ONE_MINUTE
60
```

A aplicação deverá usar um padrão interno.

Para o MVP:

```text
M1
```

Exemplo conceitual:

```csharp
public interface ITimeframeNormalizer
{
    string Normalize(string providerTimeframe);
}
```

---

## 12. Deduplicação

Um candle será considerado duplicado quando possuir a mesma combinação:

```text
Symbol + Timeframe + OpenTime
```

Regra:

```text
Não podem existir dois candles definitivos com o mesmo Symbol, Timeframe e OpenTime.
```

O banco deverá ter índice único para garantir essa regra.

A camada de persistência deverá tratar duplicidade sem derrubar a aplicação.

---

## 13. Estratégia AddOrUpdate

Ao receber um candle já existente:

1. Se o candle existente for definitivo e o novo também for definitivo, a aplicação poderá ignorar ou atualizar conforme configuração.
2. Se o candle existente estiver incompleto e o novo estiver fechado, a aplicação deverá atualizar.
3. Se o candle novo for em formação, ele não deverá sobrescrever candle definitivo.

Para o MVP, a regra padrão será:

```text
Candles fechados duplicados devem ser ignorados se já existirem no banco.
```

---

## 14. Backfill inicial

Ao iniciar pela primeira vez, a aplicação poderá não ter candles suficientes para análise.

Nesse caso, deverá executar um backfill inicial.

Objetivo:

```text
Preencher o histórico mínimo necessário para análise técnica.
```

A quantidade mínima de candles deverá considerar:

1. janela da regressão linear;
2. maior período de EMA;
3. margem de segurança.

Fórmula sugerida:

```text
MinimumRequiredCandles = max(RegressionWindowCandles, MaxEmaPeriod) + SafetyMargin
```

Exemplo:

```text
RegressionWindowCandles = 120
MaxEmaPeriod = 50
SafetyMargin = 20

MinimumRequiredCandles = 140
```
Nota: Os valores de RegressionWindowCandles e MaxEmaPeriod são definidos nas SPECs `0001-market-radar-web-mvp.md` e `0004-trend-analysis-engine.md` respectivamente

---

## 15. Backfill de 30 dias

A configuração `HistoryDays = 30` define o histórico desejado.

Porém, para o MVP, a aplicação poderá operar com dois níveis:

1. histórico mínimo para análise;
2. histórico completo desejado.

Regras:

1. Se não houver candles suficientes para análise, buscar o mínimo necessário.
2. Se o provider permitir, preencher gradualmente os 30 dias.
3. A ausência dos 30 dias completos não deve impedir o sistema de funcionar, desde que exista o mínimo necessário para análise.
4. O dashboard deverá indicar quando o histórico ainda estiver incompleto.

---

## 16. Falha do provider

Se o provider falhar, a aplicação deverá:

1. registrar erro em log;
2. não apagar dados anteriores;
3. não gerar alerta com dados incompletos;
4. tentar novamente no próximo ciclo;
5. informar status de indisponibilidade no dashboard;
6. manter a aplicação em execução;
7. A falha de provider não deve derrubar o background worker;
8. Não deve gerar alerta quando a coleta falhar.
---

## 17. Dados incompletos

Se a aplicação não tiver candles suficientes para análise, ela deverá retornar status explícito.

Exemplo:

```text
Status: WaitingForEnoughData
Mensagem: Ainda não há candles suficientes para análise.
```

Nesse cenário:

1. não calcular regressão;
2. não calcular score de alerta;
3. não gerar alerta técnico;
4. continuar coletando candles.

---

## 18. Status de qualidade dos dados

A aplicação deverá representar o status da qualidade dos dados.

Sugestão de enum:

```csharp
public enum MarketDataQualityStatus
{
    Unknown = 0,
    Valid = 1,
    WaitingForEnoughData = 2,
    ProviderUnavailable = 3,
    InvalidCandlesReceived = 4,
    StaleData = 5
}
```

---

## 19. Stale data

Dados serão considerados obsoletos quando o último candle fechado estiver muito distante do horário atual.

Exemplo:

```text
Timeframe: M1
Tolerância: 3 minutos
```

Se o último candle fechado tiver mais de 3 minutos de atraso, o status deverá ser:

```text
StaleData
```

Dados obsoletos não devem gerar novo alerta técnico.

---

## 20. Contratos sugeridos

## 20.1 ICandleValidator

```csharp
public interface ICandleValidator
{
    CandleValidationResult Validate(MarketCandle candle);
}
```

---

## 20.2 IMarketDataQualityService

```csharp
public interface IMarketDataQualityService
{
    MarketDataQualityResult Evaluate(
        IReadOnlyCollection<MarketCandle> candles,
        MarketDataQualitySettings settings);
}
```

---

## 20.3 MarketDataQualityResult

```csharp
public class MarketDataQualityResult
{
    public MarketDataQualityStatus Status { get; set; }
    public bool CanAnalyze { get; set; }
    public IReadOnlyCollection<string> Messages { get; set; }
}
```

---

## 20.4 MarketDataQualitySettings

```csharp
public class MarketDataQualitySettings
{
    public int MinimumRequiredCandles { get; set; }
    public int StaleDataToleranceMinutes { get; set; }
    public bool UseOnlyClosedCandlesForAnalysis { get; set; }
}
```

---

## 21. Configuração sugerida

Adicionar ao `appsettings.json`:

```json
{
  "MarketDataQuality": {
    "UseOnlyClosedCandlesForAnalysis": true,
    "SafetyMarginCandles": 20,
    "StaleDataToleranceMinutes": 3,
    "RejectInvalidCandles": true
  }
}
```

---

## 22. Fluxo de validação

```text
Provider retorna candles
↓
Normalizar símbolo
↓
Normalizar timeframe
↓
Converter datas para UTC
↓
Converter preços para decimal
↓
Separar candle fechado de candle em formação
↓
Validar regras de qualidade
↓
Descartar candles inválidos
↓
Persistir candles fechados válidos
↓
Avaliar se há candles suficientes
↓
Liberar ou bloquear análise técnica
```

---

## 23. Critérios de aceite

## Cenário 1 — Candle válido

Dado um candle com preços positivos e consistentes  
Quando a validação for executada  
Então o candle deverá ser considerado válido.

---

## Cenário 2 — Candle com preço negativo

Dado um candle com `ClosePrice` negativo  
Quando a validação for executada  
Então o candle deverá ser rejeitado.

---

## Cenário 3 — Candle com máxima menor que mínima

Dado um candle onde `HighPrice` é menor que `LowPrice`  
Quando a validação for executada  
Então o candle deverá ser rejeitado.

---

## Cenário 4 — Candle em formação

Dado um candle ainda em formação  
Quando a análise técnica for executada  
Então esse candle não deverá ser usado para gerar alertas.

---

## Cenário 5 — Candle duplicado

Dado que já existe um candle com mesmo símbolo, timeframe e horário  
Quando um novo candle igual for recebido  
Então a aplicação não deverá criar duplicidade.

---

## Cenário 6 — Histórico insuficiente

Dado que existem menos candles que o mínimo necessário  
Quando a análise for solicitada  
Então a aplicação deverá retornar `WaitingForEnoughData`.

---

## Cenário 7 — Provider indisponível

Dado que o provider falhou  
Quando o worker executar  
Então a aplicação deverá registrar erro, manter dados anteriores e continuar no próximo ciclo.

---

## Cenário 8 — Dados obsoletos

Dado que o último candle fechado está atrasado além da tolerância configurada  
Quando a qualidade dos dados for avaliada  
Então o status deverá ser `StaleData`.

---

## 24. Testes mínimos esperados

## 24.1 Testes de validação de candle

Validar:

1. candle válido;
2. preço zero;
3. preço negativo;
4. máxima menor que mínima;
5. abertura fora do range;
6. fechamento fora do range;
7. volume negativo;
8. símbolo vazio;
9. timeframe vazio;
10. horário futuro.

---

## 24.2 Testes de candle fechado

Validar:

1. candle M1 fechado;
2. candle M1 em formação;
3. candle em formação não usado para análise;
4. candle fechado usado para análise.

---

## 24.3 Testes de qualidade de dados

Validar:

1. histórico suficiente;
2. histórico insuficiente;
3. provider indisponível;
4. dados obsoletos;
5. candles inválidos recebidos.

---

## 24.4 Testes de normalização

Validar:

1. normalização de símbolo;
2. normalização de timeframe;
3. conversão de datas para UTC;
4. conversão de preços para decimal.

---

## 25. Decisões importantes

1. Candles fechados serão a base oficial da análise.
2. Candles em formação poderão ser exibidos, mas não usados em alertas.
3. Todos os horários serão armazenados em UTC.
4. Todos os preços internos serão `decimal`.
5. Dados inválidos não devem gerar alertas.
6. Falha de provider não deve derrubar a aplicação.
7. Histórico insuficiente deve bloquear análise técnica.
8. Dados obsoletos devem bloquear novos alertas.
9. A qualidade dos dados deve ser avaliada antes da análise técnica.

---

## 26. Resultado esperado

Ao final desta SPEC, a aplicação deverá possuir uma camada confiável de qualidade de dados capaz de:

1. validar candles;
2. normalizar símbolo e timeframe;
3. padronizar horários em UTC;
4. separar candles fechados de candles em formação;
5. bloquear análise com dados insuficientes;
6. bloquear análise com dados inválidos;
7. bloquear alertas com dados obsoletos;
8. proteger o sistema contra falsos alertas causados por dados ruins.
