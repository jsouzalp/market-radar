# Market Radar

Aplicação Blazor Server em .NET 10 que monitora o par **XAUUSD (ouro)** em tempo real, detecta quebras de tendência por regressão linear e EMAs, e gera alertas visuais e sonoros.

> Apenas alertas — nenhuma execução de ordens.

---

## Funcionalidades

- Coleta de candles M1 via provider configurável (mock, MetaTrader, CSV, etc.)
- Regressão linear sobre janela de 120 candles para detecção de tendência
- Cálculo de EMAs (9, 21, 50) sobre o histórico completo
- Detecção de **TrendBreakDown** e **TrendBreakUp** com pontuação 0–100
- Dashboard em tempo real com gráfico de preço, linha de tendência e EMAs
- Alertas com badge de severidade (Atenção / Alerta / Alerta Forte)
- Alerta sonoro opcional
- Cooldown configurável para evitar spam de alertas consecutivos
- Barra de progresso durante aquecimento de dados
- Estado "Mercado Fechado" quando o provider não retorna candles

## Stack

| Camada | Tecnologia |
|--------|-----------|
| Framework | .NET 10 / ASP.NET Core |
| UI | Blazor Server (`@rendermode InteractiveServer`) |
| Banco de dados | SQLite via EF Core 10 |
| Testes | xUnit + FluentAssertions |
| Estilo | Bootstrap 5 |

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Nenhuma instalação de banco de dados necessária — o SQLite é criado automaticamente

---

## Início rápido

```bash
# Clone o repositório
git clone <url-do-repo>
cd CoinMarket

# Restaurar dependências e compilar
dotnet build MarketRadar.slnx

# Executar a aplicação
dotnet run --project src/MarketRadar.Web
```

A aplicação sobe em `https://localhost:5001` (ou a porta configurada). O banco `marketradar.db` é criado automaticamente na primeira execução.

---

## Configuração

Edite `src/MarketRadar.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "MarketRadar": "Data Source=marketradar.db"
  },
  "MarketMonitor": {
    "HistoryDays": 30,
    "Symbols": [{ "Code": "XAUUSD", "Enabled": true, "Timeframe": "M1" }]
  },
  "MarketDataProvider": {
    "ActiveProvider": "Mock",
    "PollingIntervalSeconds": 60,
    "MockScenario": "TrendBreakDown",
    "UseOnlyClosedCandles": true,
    "RequestOverlapMinutes": 5
  },
  "TrendAnalysis": {
    "RegressionWindowCandles": 120,
    "RequiredBreakCandles": 3,
    "DeviationMultiplier": 1.5,
    "MinimumSlope": 0.001,
    "MinimumBreakScore": 70
  },
  "MarketDataQuality": {
    "MinimumRequiredCandles": 120,
    "StaleDataToleranceMinutes": 5
  },
  "AlertCooldown": {
    "MaxConsecutiveAlerts": 5,
    "MinCyclesToReset": 3
  }
}
```

### Providers disponíveis (`ActiveProvider`)

| Valor | Descrição |
|-------|-----------|
| `Mock` | Geração sintética de candles (ideal para desenvolvimento) |
| `MetaTrader` | Integração via MetaTrader |
| `FotMarket` | API FotMarket |
| `ExternalHttp` | Provider HTTP genérico |
| `Csv` | Leitura de arquivo CSV |

### Cenários do Mock (`MockScenario`)

| Valor | Comportamento |
|-------|--------------|
| `TrendBreakDown` | Tendência de alta seguida de quebra para baixo |
| `TrendBreakUp` | Tendência de baixa seguida de quebra para cima |
| `UpTrend` | Alta contínua sem quebra |
| `DownTrend` | Baixa contínua sem quebra |
| `StableMarket` | Preço oscilando lateralmente |
| `EmptyResponse` / `MarketClosed` | Retorna sem candles — simula mercado fechado |
| `ProviderFailure` | Lança exceção — simula falha do provider |

---

## Score de alerta

| Condição | Pontos |
|----------|--------|
| Quebrou a linha de regressão | +25 |
| Manteve a quebra por K candles consecutivos | +20 |
| Distância acima do limiar estatístico | +20 |
| Preço cruzou EMA 21 | +15 |
| EMA 9 virou contra a tendência anterior | +10 |
| Quebrou máxima/mínima recente | +10 |

| Faixa | Ação |
|-------|------|
| 0–49 | Ignorado |
| 50–69 | Atenção |
| 70–84 | Alerta |
| 85–100 | Alerta forte |

---

## Testes

```bash
# Todos os testes (82 no total)
dotnet test MarketRadar.slnx

# Apenas testes de domínio (58 — sem banco, sem DI)
dotnet test tests/MarketRadar.Domain.Tests

# Apenas testes de integração de Application (24 — SQLite in-memory)
dotnet test tests/MarketRadar.Application.Tests

# Filtrar por nome
dotnet test --filter "FullyQualifiedName~TrendBreakDown"
```

Os testes de integração usam SQLite in-memory via `AppTestFixture` — nenhuma configuração externa necessária.

---

## Arquitetura

```
MarketRadar.slnx
├── src/
│   ├── MarketRadar.Domain          # Lógica pura — sem dependências externas
│   ├── MarketRadar.Application     # Contratos, orchestration, ViewModels, settings
│   ├── MarketRadar.Infrastructure  # EF Core, repositórios, worker, providers
│   └── MarketRadar.Web             # Blazor Server, dashboard, sem lógica de negócio
└── tests/
    ├── MarketRadar.Domain.Tests    # Testes unitários de serviços de domínio
    └── MarketRadar.Application.Tests  # Testes de integração com SQLite in-memory
```

Regra de dependência: `Web → Application → Domain`. `Infrastructure` implementa contratos definidos em `Application` e nunca é referenciada por `Application` ou `Domain`.

---

## Especificações

Decisões de design e critérios de aceite estão documentados em `.specs/`:

| Arquivo | Conteúdo |
|---------|---------|
| `0001-market-radar-web-mvp.md` | Escopo do MVP, modelos, fluxos |
| `0002-market-data-provider.md` | Abstração de provider, normalização |
| `0003-market-data-quality.md` | Validação de candles, estados de qualidade |
| `0004-trend-analysis-engine.md` | Regressão, EMA, detecção, scoring |
| `0005-dashboard-and-alerts.md` | UI Blazor, estados do dashboard, dispatch |
| `0006-arch-decisions.md` | Cooldown, MarketClosed, reestruturação de config |
| `0007-implementation-plan.md` | Plano de implementação retrospectivo |
