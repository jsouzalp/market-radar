# SPEC 0007 — Implementation Plan

## 1. Objetivo

Definir o plano de implementação do projeto Market Radar, organizando a execução das SPECs anteriores em fases claras, sequenciais e testáveis.

Esta SPEC não adiciona novas funcionalidades ao produto. Ela define **como implementar** o MVP com menor risco técnico, evitando que a construção comece pela interface ou por integrações externas antes da base de domínio estar validada.

Esta SPEC complementa:

```text
0001-market-radar-web-mvp.md
0002-market-data-provider.md
0003-market-data-quality.md
0004-trend-analysis-engine.md
0005-dashboard-and-alerts.md
0006-arch-decisions.md
```

---

## 2. Princípio principal

A implementação deverá seguir a ordem:

```text
Domínio primeiro
↓
Serviços de análise
↓
Qualidade dos dados
↓
Provider mockado
↓
Persistência
↓
Worker
↓
Dashboard
↓
Alertas
```

A interface web não deverá ser implementada antes das regras centrais estarem testadas.

---

## 3. Objetivo do MVP implementável

Ao final do plano, a aplicação deverá:

1. Executar como web app em .NET 10 com Blazor Server.
2. Monitorar inicialmente `XAUUSD` no timeframe `M1`.
3. Usar `MockMarketDataProvider` como provider inicial.
4. Construir histórico organicamente, sem backfill inicial.
5. Validar candles antes da análise.
6. Bloquear análise até atingir `MinimumRequiredCandles`.
7. Calcular regressão linear.
8. Calcular EMA 9, EMA 21 e EMA 50.
9. Detectar rompimento de tendência.
10. Calcular score técnico.
11. Persistir candles e alertas.
12. Exibir dashboard simples.
13. Exibir estado `WaitingForEnoughData` com progresso.
14. Tratar `MarketClosed` como estado normal.
15. Aplicar cooldown de alertas consecutivos.
16. Emitir alerta visual e sonoro.
17. Não executar ordens.
18. Não depender de IA.

---

## 4. Fora do escopo da implementação inicial

Durante a execução deste plano, não implementar:

1. Compra ou venda automática.
2. IA para explicar alertas.
3. Integração real com corretora.
4. Troca de provider via dashboard.
5. Backfill inicial de 30 dias.
6. Estratégia de trading automatizada.
7. Dashboard avançado.
8. Multiusuário.
9. Autenticação.
10. Notificação por WhatsApp, Telegram ou e-mail.
11. Backtest completo.
12. Paper trading.

Esses pontos poderão ser tratados em fases futuras.

---

## 5. Fase 1 — Solution Skeleton

### 5.1 Objetivo

Criar a estrutura inicial da solução em .NET 10, separando projetos por responsabilidade.

### 5.2 Projetos

Criar:

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
    ├── MarketRadar.Application.Tests
    └── MarketRadar.Infrastructure.Tests
```

### 5.3 Regras

1. `MarketRadar.Domain` não deve depender de nenhum outro projeto.
2. `MarketRadar.Application` pode depender de `Domain`.
3. `MarketRadar.Infrastructure` pode depender de `Application` e `Domain`.
4. `MarketRadar.Web` pode depender de `Application`, `Infrastructure` e `Domain`.
5. Testes devem ser criados desde o início.

### 5.4 Critérios de conclusão

A fase estará concluída quando:

1. A solution compilar.
2. Todos os projetos estiverem referenciados corretamente.
3. O projeto Web iniciar com uma página simples.
4. O pipeline de testes executar, mesmo que ainda sem testes relevantes.

---

## 6. Fase 2 — Domain Models

### 6.1 Objetivo

Criar os modelos centrais do domínio sem dependência de banco, tela ou provider externo.

### 6.2 Implementar

Criar modelos:

```text
MarketCandle
MarketAlert
TrendLineResult
MovingAverageResult
TrendBreakAnalysisResult
MarketDataQualityResult
DashboardViewModel, se ficar em Application/Web.Shared
```

Criar enums:

```text
AlertType
AlertSeverity
TrendDirection
MovingAverageDirection
TrendAnalysisStatus
MarketDataQualityStatus
DashboardStatus
```

### 6.3 Regras

1. Usar `decimal` para preços, médias, distâncias e scores.
2. Usar `DateTime` em UTC para horários.
3. Não colocar atributos de Entity Framework no domínio neste momento.
4. Não colocar lógica de tela nos modelos de domínio.
5. Não colocar lógica de provider nos modelos.

### 6.4 Critérios de conclusão

A fase estará concluída quando:

1. Todos os modelos centrais existirem.
2. O projeto Domain compilar isoladamente.
3. Os modelos forem reutilizáveis pelas demais camadas.

---

## 7. Fase 3 — Settings Models

### 7.1 Objetivo

Criar classes fortemente tipadas para as configurações do sistema.

### 7.2 Implementar

Criar settings:

```text
MarketMonitorSettings
MarketDataProviderSettings
MarketDataQualitySettings
TrendAnalysisSettings
MovingAverageSettings
AlertSettings
AlertCooldownSettings
```

### 7.3 Configuração esperada

O `appsettings.json` inicial deverá conter:

```json
{
  "MarketMonitor": {
    "HistoryDays": 30,
    "Symbols": [
      {
        "Code": "XAUUSD",
        "Enabled": true,
        "Timeframe": "M1"
      }
    ]
  },
  "MarketDataProvider": {
    "Primary": "Mock",
    "Comparison": null,
    "ActiveProvider": "Mock",
    "PollingIntervalSeconds": 60,
    "UseOnlyClosedCandles": true,
    "RequestOverlapMinutes": 5,
    "MockScenario": "TrendBreakDown"
  },
  "MarketDataQuality": {
    "UseOnlyClosedCandlesForAnalysis": true,
    "SafetyMarginCandles": 20,
    "StaleDataToleranceMinutes": 3,
    "RejectInvalidCandles": true
  },
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
  },
  "Alerts": {
    "EnableSound": true,
    "EnableVisualHighlight": true,
    "EnableBrowserNotification": false
  },
  "AlertCooldown": {
    "MaxConsecutiveAlerts": 5,
    "MinCyclesToReset": 3
  }
}
```

### 7.4 Critérios de conclusão

A fase estará concluída quando:

1. Settings estiverem mapeados via Options Pattern.
2. A aplicação iniciar lendo as configurações.
3. Configurações inválidas forem detectadas em startup quando possível.

---

## 8. Fase 4 — Market Data Provider Mock

### 8.1 Objetivo

Implementar o provider mockado para permitir desenvolvimento sem dependência de API externa.

### 8.2 Implementar

Contrato:

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

Implementação:

```text
MockMarketDataProvider
```

Cenários mínimos:

```text
StableMarket
UpTrend
DownTrend
TrendBreakDown
TrendBreakUp
ProviderFailure
EmptyResponse
MarketClosed
```

### 8.3 Regras

1. O mock deve gerar candles M1 válidos.
2. O mock deve permitir cenários previsíveis.
3. O mock deve respeitar `from`, `to`, `symbol`, `timeframe` e `CancellationToken`.
4. O mock não deve salvar candles.
5. O mock não deve executar análise.
6. O mock não deve emitir alertas.

### 8.4 Critérios de conclusão

A fase estará concluída quando:

1. O provider mockado retornar candles válidos.
2. O cenário `TrendBreakDown` gerar dados compatíveis com rompimento de baixa.
3. O cenário `TrendBreakUp` gerar dados compatíveis com rompimento de alta.
4. O cenário `ProviderFailure` simular falha controlada.
5. O cenário `MarketClosed` retornar condição tratável sem erro fatal.

---

## 9. Fase 5 — Candle Validation and Market Data Quality

### 9.1 Objetivo

Implementar validação e avaliação de qualidade dos candles conforme a SPEC 0003.

### 9.2 Implementar

Contratos:

```csharp
public interface ICandleValidator
{
    CandleValidationResult Validate(MarketCandle candle);
}

public interface IMarketDataQualityService
{
    MarketDataQualityResult Evaluate(
        IReadOnlyCollection<MarketCandle> candles,
        MarketDataQualitySettings settings);
}
```

### 9.3 Regras obrigatórias

Validar:

1. símbolo obrigatório;
2. timeframe obrigatório;
3. preços positivos;
4. máxima maior ou igual à mínima;
5. abertura dentro do range;
6. fechamento dentro do range;
7. volume não negativo quando informado;
8. horário não futuro;
9. candles fechados para análise;
10. dados obsoletos;
11. quantidade mínima de candles.

### 9.4 Regras da SPEC 0006

1. Não executar backfill inicial.
2. Construir histórico organicamente.
3. Retornar `WaitingForEnoughData` até atingir `MinimumRequiredCandles`.
4. Tratar mercado fechado como condição normal.
5. Não gerar alerta em provider falho, mercado fechado ou dados obsoletos.

### 9.5 Critérios de conclusão

A fase estará concluída quando:

1. Candles inválidos forem rejeitados.
2. Candles em formação não forem usados para análise.
3. Histórico insuficiente retornar `WaitingForEnoughData`.
4. Dados obsoletos retornarem `StaleData`.
5. Mercado fechado retornar estado próprio, sem erro fatal.
6. Testes cobrirem os cenários principais.

---

## 10. Fase 6 — Trend Analysis Engine

### 10.1 Objetivo

Implementar o motor matemático de análise técnica conforme a SPEC 0004.

### 10.2 Implementar

Contratos:

```csharp
public interface ITrendLineService
{
    TrendLineResult Calculate(
        IReadOnlyList<MarketCandle> candles,
        TrendAnalysisSettings settings);
}

public interface IMovingAverageService
{
    IReadOnlyCollection<MovingAverageResult> CalculateEma(
        IReadOnlyList<MarketCandle> candles,
        IReadOnlyCollection<int> periods);
}

public interface ITrendScoreService
{
    decimal CalculateScore(
        TrendLineResult trendLine,
        IReadOnlyCollection<MovingAverageResult> movingAverages,
        IReadOnlyList<MarketCandle> candles,
        TrendAnalysisSettings settings);
}

public interface ITrendBreakAnalysisService
{
    TrendBreakAnalysisResult Analyze(
        IReadOnlyList<MarketCandle> candles,
        TrendAnalysisSettings trendSettings,
        MovingAverageSettings movingAverageSettings);
}
```

### 10.3 Regras

1. Regressão linear usa `ClosePrice`.
2. EMA usa `ClosePrice`.
3. Tendência neutra não gera rompimento.
4. Rompimento exige K candles consecutivos.
5. Rompimento exige distância estatística mínima.
6. EMA 21 confirma o rompimento.
7. Score mínimo decide se há alerta.
8. O motor não pode acessar banco.
9. O motor não pode acessar provider.
10. O motor não pode emitir som ou atualizar dashboard.

### 10.4 Critérios de conclusão

A fase estará concluída quando:

1. Regressão detectar alta, baixa e tendência neutra.
2. EMAs 9, 21 e 50 forem calculadas.
3. Rompimento de baixa for detectado corretamente.
4. Rompimento de alta for detectado corretamente.
5. Falso rompimento simples for ignorado.
6. Score abaixo do mínimo não gerar alerta.
7. Testes unitários cobrirem os cenários principais.

---

## 11. Fase 7 — Persistence

### 11.1 Objetivo

Implementar persistência de candles e alertas.

### 11.2 Implementar

Repositórios:

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

    Task<int> CountAsync(
        string symbol,
        string timeframe,
        CancellationToken cancellationToken);
}

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

### 11.3 Banco de dados

Criar tabelas:

```text
MarketCandles
MarketAlerts
```

Chave lógica de candle:

```text
Symbol + Timeframe + OpenTime
```

### 11.4 Regras

1. Evitar duplicidade de candle.
2. Persistir apenas candles válidos.
3. Salvar alertas gerados.
4. Não sobrescrever candle fechado com candle em formação.
5. Não apagar histórico antigo no MVP.
6. Usar migrations.

### 11.5 Critérios de conclusão

A fase estará concluída quando:

1. Candles forem persistidos.
2. Candles duplicados não criarem registros duplicados.
3. Últimos N candles forem recuperados em ordem correta.
4. Alertas forem persistidos.
5. Alertas recentes forem recuperados corretamente.
6. Testes de repositório passarem.

---

## 12. Fase 8 — Market Monitor Worker

### 12.1 Objetivo

Implementar o background worker responsável por coordenar coleta, validação, persistência, análise e geração de alertas.

### 12.2 Fluxo

```text
Ler símbolos habilitados
↓
Calcular janela de coleta
↓
Buscar candles no provider ativo
↓
Normalizar e validar candles
↓
Persistir candles válidos
↓
Contar candles disponíveis
↓
Se não houver mínimo suficiente:
    status = WaitingForEnoughData
    encerrar ciclo
↓
Buscar últimos N candles
↓
Avaliar qualidade dos dados
↓
Se qualidade bloqueia:
    encerrar ciclo
↓
Executar análise técnica
↓
Se houver alerta:
    aplicar cooldown
    persistir alerta se permitido
↓
Aguardar próximo ciclo
```

### 12.3 Regras

1. Worker não deve parar por falha de provider.
2. Worker não deve gerar alerta com dados inválidos.
3. Worker não deve gerar alerta com histórico insuficiente.
4. Worker não deve gerar alerta em mercado fechado.
5. Worker deve respeitar `PollingIntervalSeconds`.
6. Worker deve respeitar `CancellationToken`.
7. Worker deve registrar logs relevantes.
8. Worker deve aplicar cooldown de alertas.

### 12.4 Critérios de conclusão

A fase estará concluída quando:

1. O worker coletar candles periodicamente.
2. O worker persistir candles válidos.
3. O worker bloquear análise sem candles suficientes.
4. O worker executar análise ao atingir o mínimo.
5. O worker persistir alertas válidos.
6. O worker ignorar alertas bloqueados por cooldown.
7. Falha do provider não derrubar a aplicação.

---

## 13. Fase 9 — Alert Cooldown

### 13.1 Objetivo

Implementar controle de alertas consecutivos conforme SPEC 0006.

### 13.2 Implementar

Serviço sugerido:

```csharp
public interface IAlertCooldownService
{
    AlertCooldownResult Evaluate(
        MarketAlert candidateAlert,
        TrendBreakAnalysisResult analysisResult,
        AlertCooldownSettings settings);
}
```

Resultado sugerido:

```csharp
public class AlertCooldownResult
{
    public bool CanDispatch { get; set; }
    public int ConsecutiveCount { get; set; }
    public int MaxConsecutiveAlerts { get; set; }
    public string Message { get; set; }
}
```

### 13.3 Regras

1. Contabilizar alertas consecutivos do mesmo tipo para o mesmo símbolo.
2. Bloquear após `MaxConsecutiveAlerts`.
3. Resetar após `MinCyclesToReset` ciclos consecutivos abaixo do threshold.
4. Expor contador para o dashboard.
5. Não bloquear alertas de tipo diferente.
6. Não bloquear alertas de símbolo diferente.

### 13.4 Critérios de conclusão

A fase estará concluída quando:

1. Alertas consecutivos forem contados.
2. Alertas forem bloqueados ao atingir o limite.
3. Reset por ciclos abaixo do threshold funcionar.
4. Dashboard conseguir exibir contador N/Max.
5. Testes cobrirem o fluxo de bloqueio e reset.

---

## 14. Fase 10 — Dashboard App Service

### 14.1 Objetivo

Criar o serviço de aplicação que fornece os dados prontos para o dashboard.

### 14.2 Implementar

Contrato:

```csharp
public interface IDashboardAppService
{
    Task<DashboardViewModel> GetDashboardAsync(
        string symbol,
        string timeframe,
        CancellationToken cancellationToken);
}
```

### 14.3 Regras

1. O dashboard app service pode consultar repositórios.
2. O dashboard app service pode montar ViewModels.
3. O dashboard app service não deve recalcular regra técnica complexa se já houver resultado persistido/disponível.
4. A UI não deve acessar repositórios diretamente.
5. A UI não deve acessar provider diretamente.
6. A UI não deve calcular regressão, EMA ou score.

### 14.4 Estados suportados

O dashboard deverá representar:

```text
Loading
Online
WaitingForEnoughData
ProviderUnavailable
StaleData
MarketClosed
Error
```

### 14.5 Critérios de conclusão

A fase estará concluída quando:

1. O serviço retornar dados do dashboard.
2. O serviço retornar progresso durante `WaitingForEnoughData`.
3. O serviço retornar estado `MarketClosed`.
4. O serviço retornar alertas recentes.
5. O serviço retornar pontos para gráfico.
6. O serviço retornar contador de cooldown.

---

## 15. Fase 11 — Dashboard UI

### 15.1 Objetivo

Implementar a tela web simples do Market Radar conforme SPEC 0005.

### 15.2 Implementar componentes

```text
Dashboard.razor
StatusCard.razor
TrendCard.razor
MovingAverageCard.razor
AlertPanel.razor
RecentAlertsTable.razor
MarketChart.razor
WaitingForEnoughDataProgress.razor
ProviderStatusBadge.razor
AlertCooldownIndicator.razor
```

### 15.3 Regras

1. Tela apenas exibe dados.
2. Tela não calcula regra técnica.
3. Tela mostra horários em UTC.
4. Tela mostra nota: "Horários exibidos em UTC."
5. Tela mostra progresso durante aquecimento.
6. Tela não exibe gráfico, score ou alerta técnico durante `WaitingForEnoughData`.
7. Tela exibe `MarketClosed` como condição normal.
8. Tela não usa mensagens de compra/venda.

### 15.4 Critérios de conclusão

A fase estará concluída quando:

1. Dashboard exibir cards principais.
2. Dashboard exibir progresso de candles coletados.
3. Dashboard exibir gráfico quando online.
4. Dashboard exibir alertas recentes.
5. Dashboard exibir status de provider.
6. Dashboard exibir mercado fechado.
7. Dashboard atualizar automaticamente.

---

## 16. Fase 12 — Visual and Sound Alerts

### 16.1 Objetivo

Implementar comportamento visual e sonoro para alertas.

### 16.2 Regras

1. Som apenas para alerta novo.
2. Som não deve repetir para o mesmo alerta.
3. Som respeita `EnableSound`.
4. Destaque visual respeita `EnableVisualHighlight`.
5. Alerta crítico deve ser mais evidente que alerta informativo.
6. Browser notification fica fora do MVP, mesmo existindo configuração.

### 16.3 Critérios de conclusão

A fase estará concluída quando:

1. Alerta novo destacar painel.
2. Alerta novo aparecer na lista.
3. Alerta novo aparecer no gráfico, se aplicável.
4. Som tocar uma única vez para alerta novo.
5. Som não tocar se desabilitado.
6. Som não repetir para mesmo alerta.

---

## 17. Fase 13 — Logging and Observability

### 17.1 Objetivo

Adicionar logs suficientes para entender o comportamento da aplicação.

### 17.2 Logs mínimos

Registrar:

1. início e fim do ciclo do worker;
2. provider ativo;
3. quantidade de candles recebidos;
4. quantidade de candles válidos;
5. candles rejeitados;
6. status de qualidade;
7. `WaitingForEnoughData`;
8. `MarketClosed`;
9. análise executada;
10. alerta gerado;
11. alerta bloqueado por cooldown;
12. falhas de provider;
13. exceções inesperadas.

### 17.3 Critérios de conclusão

A fase estará concluída quando:

1. Logs permitirem diagnosticar falhas básicas.
2. Logs não expuserem dados sensíveis.
3. Logs não ficarem excessivamente ruidosos em operação normal.

---

## 18. Fase 14 — Tests and Validation

### 18.1 Objetivo

Consolidar testes unitários e de integração.

### 18.2 Testes obrigatórios

Testar:

1. validação de candles;
2. quality status;
3. provider mockado;
4. regressão linear;
5. EMA;
6. rompimento de alta;
7. rompimento de baixa;
8. tendência neutra;
9. score insuficiente;
10. persistência sem duplicidade;
11. worker com provider falho;
12. worker com mercado fechado;
13. cooldown;
14. dashboard app service.

### 18.3 Critérios de conclusão

A fase estará concluída quando:

1. Testes principais passarem.
2. O projeto compilar sem warnings relevantes.
3. O fluxo do MVP funcionar com `MockMarketDataProvider`.
4. O dashboard exibir dados reais do mock.
5. Nenhuma regra técnica estiver implementada diretamente na UI.

---

## 19. Ordem resumida de execução

```text
1. Solution Skeleton
2. Domain Models
3. Settings Models
4. Market Data Provider Mock
5. Candle Validation and Market Data Quality
6. Trend Analysis Engine
7. Persistence
8. Market Monitor Worker
9. Alert Cooldown
10. Dashboard App Service
11. Dashboard UI
12. Visual and Sound Alerts
13. Logging and Observability
14. Tests and Validation
```

---

## 20. Instrução para execução com IA/Codex

Ao usar IA/Codex para implementar, enviar uma fase por vez.

Não pedir:

```text
Implemente o projeto inteiro.
```

Preferir comandos como:

```text
Implemente apenas a Fase 1 da SPEC 0007.
Não avance para as próximas fases.
Garanta que a solution compile.
```

Depois:

```text
Implemente apenas a Fase 2 da SPEC 0007.
Use os contratos e decisões das SPECs 0001 a 0006.
Não implemente tela, banco ou provider real ainda.
```

---

## 21. Regras de parada

Ao final de cada fase, a IA/Codex deverá informar:

1. arquivos criados;
2. arquivos alterados;
3. decisões tomadas;
4. testes criados;
5. testes executados;
6. pendências;
7. próximos passos sugeridos.

Não avançar para a fase seguinte sem validação humana.

---

## 22. Riscos de implementação

### 22.1 Começar pelo dashboard

Risco:

```text
Criar interface bonita com regra técnica fraca ou inexistente.
```

Mitigação:

```text
Implementar dashboard apenas após domínio, análise, qualidade e worker.
```

---

### 22.2 Misturar provider com análise

Risco:

```text
Provider virar responsável por cálculo técnico.
```

Mitigação:

```text
Provider só retorna candles normalizados.
```

---

### 22.3 Gerar alerta com candle em formação

Risco:

```text
Falso alerta que aparece e desaparece.
```

Mitigação:

```text
Análise usa apenas candles fechados.
```

---

### 22.4 Flood de alertas

Risco:

```text
Vários alertas repetidos no mesmo rompimento.
```

Mitigação:

```text
Aplicar AlertCooldown.
```

---

### 22.5 Backfill pesado

Risco:

```text
Estourar limite de provider gratuito ou carregar dados inconsistentes.
```

Mitigação:

```text
Não executar backfill inicial. Histórico é orgânico.
```

---

## 23. Resultado esperado

Ao final da implementação guiada por esta SPEC, o projeto deverá possuir um MVP funcional e testável, capaz de:

1. iniciar como aplicação web;
2. gerar candles simulados;
3. validar dados;
4. acumular histórico organicamente;
5. aguardar candles suficientes;
6. calcular tendência;
7. detectar rompimento;
8. gerar alertas com cooldown;
9. persistir candles e alertas;
10. exibir dashboard básico;
11. emitir alerta visual e sonoro;
12. manter separação clara entre domínio, aplicação, infraestrutura e interface.
