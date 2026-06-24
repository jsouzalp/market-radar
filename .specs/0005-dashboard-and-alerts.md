# SPEC 0005 — Dashboard and Alerts

## 1. Objetivo

Definir o dashboard web e o comportamento dos alertas visuais e sonoros da aplicação Market Radar.

Esta SPEC complementa:

```text
0001-market-radar-web-mvp.md
0002-market-data-provider.md
0003-market-data-quality.md
0004-trend-analysis-engine.md
```

A SPEC 0005 trata da camada visual e de interação do usuário, sem conter regra de análise técnica.

---

## 2. Escopo

Esta SPEC cobre:

1. Layout inicial do dashboard.
2. Cards de status.
3. Gráfico de preço, tendência e EMAs.
4. Lista de alertas recentes.
5. Alerta visual.
6. Alerta sonoro.
7. Estados da tela.
8. Atualização automática.
9. Separação entre UI e regra de negócio.

---

## 3. Fora do escopo

Esta SPEC não define:

1. Coleta de candles.
2. Validação de candles.
3. Cálculo de regressão linear.
4. Cálculo de EMA.
5. Score técnico.
6. IA para explicação.
7. Envio de alertas por WhatsApp, Telegram ou e-mail.
8. Execução de ordens.

---

## 4. Tipo de aplicação

Para o MVP, a interface será implementada em:

```text
Blazor Server
```

Motivo:

1. mantém o projeto em .NET;
2. evita frontend separado;
3. permite atualização reativa;
4. combina bem com serviços internos;
5. facilita evolução futura.

---

## 5. Princípio de design

O dashboard deve responder rapidamente a três perguntas:

```text
1. O ativo está subindo, caindo ou lateral?
2. Houve rompimento técnico relevante?
3. Preciso prestar atenção agora?
```

A tela não deve tentar convencer o usuário a operar.

O dashboard deve comunicar:

```text
Alerta técnico, não ordem de compra ou venda.
```

---

## 6. Tela principal

A tela principal será:

```text
/pages/dashboard
```

ou equivalente em Blazor:

```text
Dashboard.razor
```

---

## 7. Estrutura visual sugerida

```text
Dashboard
│
├── Header
│   ├── Nome da aplicação
│   ├── Ativo selecionado
│   └── Status do provider
│
├── Cards de status
│   ├── Preço atual
│   ├── Variação do último candle
│   ├── Direção da tendência
│   ├── Score atual
│   └── Última atualização
│
├── Gráfico principal
│   ├── ClosePrice
│   ├── Linha de regressão
│   ├── EMA 9
│   ├── EMA 21
│   ├── EMA 50
│   └── Marcadores de alerta
│
├── Painel de alerta ativo
│
└── Lista de alertas recentes
```

---

## 8. Header

O header deverá exibir:

1. nome da aplicação;
2. símbolo monitorado;
3. timeframe;
4. status do provider;
5. horário da última coleta.

Exemplo:

```text
Market Radar | XAUUSD | M1 | Provider: Online | Última coleta: 14:35:00
```

---

## 9. Cards de status

O dashboard deverá exibir cards com informações rápidas.

## 9.1 Card Preço atual

Campos:

1. símbolo;
2. preço atual;
3. horário do candle.

Exemplo:

```text
XAUUSD
Preço atual: 2348.72
Candle: 14:35 UTC
```

---

## 9.2 Card Variação

Campos:

1. variação absoluta em relação ao candle anterior;
2. variação percentual;
3. direção.

Exemplo:

```text
Variação: -1.25
Percentual: -0.053%
Direção: queda
```

---

## 9.3 Card Tendência

Campos:

1. direção da regressão;
2. slope;
3. distância do preço até a linha;
4. desvio padrão dos resíduos.

Exemplo:

```text
Tendência: alta
Slope: 0.014
Distância: -2.10
Desvio: 1.30
```

---

## 9.4 Card Médias móveis

Campos:

1. EMA 9;
2. EMA 21;
3. EMA 50;
4. preço acima/abaixo da EMA 21.

Exemplo:

```text
EMA 9: 2350.10
EMA 21: 2349.40
EMA 50: 2345.80
Preço abaixo da EMA 21
```

---

## 9.5 Card Score

Campos:

1. score atual;
2. severidade;
3. status da análise.

Exemplo:

```text
Score: 78
Severidade: Warning
Status: BreakoutDetected
```

---

## 10. Gráfico principal

O gráfico principal deverá exibir:

1. preço de fechamento;
2. linha de regressão;
3. EMA 9;
4. EMA 21;
5. EMA 50;
6. marcadores de alerta.

Para o MVP, o gráfico poderá ser de linha.

Evolução futura:

```text
Candlestick completo.
```

Biblioteca recomendada:

```text
TradingView Lightweight Charts
```

Motivo:

1. leve;
2. adequada para mercado financeiro;
3. suporta séries de linha;
4. suporta candlestick;
5. suporta marcadores;
6. permite evolução sem trocar biblioteca.

---

## 11. Dados esperados pelo gráfico

Criar um modelo de visualização.

```csharp
public class ChartPointViewModel
{
    public DateTime Time { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal? TrendLinePrice { get; set; }
    public decimal? Ema9 { get; set; }
    public decimal? Ema21 { get; set; }
    public decimal? Ema50 { get; set; }
    public bool HasAlert { get; set; }
    public AlertType? AlertType { get; set; }
}
```

---

## 12. Painel de alerta ativo

Quando houver alerta, o dashboard deverá exibir um painel destacado.

Campos:

1. horário;
2. símbolo;
3. tipo do alerta;
4. severidade;
5. score;
6. mensagem técnica.

Exemplo:

```text
ALERTA — XAUUSD — Rompimento de baixa

Score: 78
Severidade: Warning

Os últimos 3 candles fecharam abaixo da linha de regressão e o preço atual está abaixo da EMA 21.
```

O painel não deve usar frases como:

```text
Venda agora
Compra garantida
Lucro provável
```

---

## 13. Lista de alertas recentes

A tela deverá exibir os últimos alertas.

Campos:

1. horário;
2. símbolo;
3. timeframe;
4. tipo;
5. severidade;
6. score;
7. mensagem resumida.

A lista deve ser ordenada do mais recente para o mais antigo.

---

## 14. Alerta visual

Quando um novo alerta for recebido:

1. o painel de alerta ativo deve ser atualizado;
2. a linha do alerta deve ser destacada na lista;
3. o marcador deve aparecer no gráfico;
4. o card de score deve refletir o novo estado.

A severidade deve influenciar o destaque visual.

Sugestão:

```text
Info    => destaque leve
Warning => destaque médio
Critical => destaque forte
```

---

## 15. Alerta sonoro

Se `EnableSound` estiver habilitado, o dashboard deverá emitir som quando um novo alerta relevante chegar.

Regras:

1. Som apenas para novo alerta.
2. Não repetir som a cada atualização do mesmo alerta.
3. Não tocar som para status sem alerta.
4. Permitir desabilitar som via configuração.
5. Evitar tocar som antes de interação do usuário se o navegador bloquear autoplay.

Configuração:

```json
{
  "Alerts": {
    "EnableSound": true,
    "EnableVisualHighlight": true,
    "EnableBrowserNotification": false
  }
}
```

---

## 16. Identificação de alerta novo

Um alerta será considerado novo quando possuir um `Id` ainda não exibido na tela.

A tela deverá manter controle do último alerta exibido.

Exemplo:

```text
LastDisplayedAlertId
```

Se o alerta atual possuir o mesmo Id, não repetir som.

---

## 17. Estados da tela

O dashboard deverá suportar os seguintes estados:

```csharp
public enum DashboardStatus
{
    Loading = 0,
    Online = 1,
    WaitingForEnoughData = 2,
    ProviderUnavailable = 3,
    StaleData = 4,
    Error = 5
}
```

---

## 18. Estado Loading

Exibir quando a tela ainda estiver carregando dados.

Mensagem sugerida:

```text
Carregando dados do mercado...
```

---

## 19. Estado WaitingForEnoughData

Exibir quando ainda não houver candles suficientes.

Mensagem sugerida:

```text
Ainda não há candles suficientes para análise técnica.
```

A tela pode exibir o gráfico parcial, mas não deve indicar rompimento.

---

## 20. Estado ProviderUnavailable

Exibir quando o provider estiver indisponível.

Mensagem sugerida:

```text
Provider de mercado indisponível. A aplicação tentará novamente no próximo ciclo.
```

Não gerar alerta técnico novo nesse estado.

---

## 21. Estado StaleData

Exibir quando os dados estiverem obsoletos.

Mensagem sugerida:

```text
Dados atrasados. Último candle fechado está fora da tolerância configurada.
```

Não gerar alerta técnico novo nesse estado.

---

## 22. Estado Online

Exibir quando:

1. provider está funcionando;
2. dados estão válidos;
3. há candles suficientes;
4. análise técnica foi executada.

---

## 23. Atualização automática

O dashboard deverá atualizar os dados sem exigir refresh manual.

Opções:

1. timer simples no Blazor;
2. SignalR;
3. eventos internos de aplicação.

Para o MVP, pode ser usado timer.

Recomendação:

```text
Timer no Blazor no MVP.
SignalR como evolução futura se necessário.
```

Motivo:

```text
Timer é mais simples e suficiente para polling de 60 segundos.
```

---

## 24. Intervalo de atualização da tela

A atualização da tela pode seguir o intervalo configurado do monitor.

Exemplo:

```text
PollingIntervalSeconds = 60
```

Regra:

```text
A tela não precisa atualizar mais rápido que a coleta de candles.
```

---

## 25. ViewModel principal

```csharp
public class DashboardViewModel
{
    public string Symbol { get; set; }
    public string Timeframe { get; set; }
    public DashboardStatus Status { get; set; }
    public decimal? CurrentPrice { get; set; }
    public decimal? PreviousClosePrice { get; set; }
    public decimal? AbsoluteVariation { get; set; }
    public decimal? PercentageVariation { get; set; }
    public TrendLineViewModel TrendLine { get; set; }
    public IReadOnlyCollection<MovingAverageViewModel> MovingAverages { get; set; }
    public TrendBreakViewModel CurrentAnalysis { get; set; }
    public MarketAlertViewModel LastAlert { get; set; }
    public IReadOnlyCollection<MarketAlertViewModel> RecentAlerts { get; set; }
    public IReadOnlyCollection<ChartPointViewModel> ChartPoints { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}
```

---

## 26. Serviços da aplicação para dashboard

A tela deverá consumir um serviço de aplicação.

```csharp
public interface IDashboardAppService
{
    Task<DashboardViewModel> GetDashboardAsync(
        string symbol,
        string timeframe,
        CancellationToken cancellationToken);
}
```

A tela não deverá buscar diretamente repositórios ou calcular regra técnica.

---

## 27. Separação obrigatória

O componente web não deve:

1. calcular regressão linear;
2. calcular EMA;
3. calcular score;
4. consultar provider externo diretamente;
5. decidir se há rompimento;
6. persistir candles.

O componente web deve apenas:

1. chamar `IDashboardAppService`;
2. exibir dados;
3. emitir som;
4. destacar visualmente alertas.

---

## 28. Componente sugerido

Estrutura sugerida:

```text
MarketRadar.Web
│
├── Pages
│   └── Dashboard.razor
│
├── Components
│   ├── StatusCard.razor
│   ├── TrendCard.razor
│   ├── MovingAverageCard.razor
│   ├── AlertPanel.razor
│   ├── RecentAlertsTable.razor
│   └── MarketChart.razor
│
└── wwwroot
    ├── js
    │   └── market-chart.js
    └── sounds
        └── alert.mp3
```

---

## 29. JavaScript interop

Para o gráfico e som, poderá ser usado JS interop.

Usos:

1. renderizar gráfico financeiro;
2. atualizar séries;
3. tocar som;
4. controlar notificações do navegador futuramente.

O JS interop deve ficar isolado em arquivos próprios.

---

## 30. Gráfico com TradingView Lightweight Charts

O componente `MarketChart.razor` deverá receber dados já prontos para visualização.

Ele não deverá calcular indicadores.

Séries esperadas:

1. ClosePrice;
2. TrendLinePrice;
3. EMA 9;
4. EMA 21;
5. EMA 50.

Marcadores esperados:

1. TrendBreakDown;
2. TrendBreakUp;
3. PossibleFalseBreakout, futuro.

---

## 31. Histórico visual de alertas

O gráfico deverá marcar alertas passados.

Para cada alerta:

1. localizar candle correspondente ao horário do alerta;
2. exibir marcador;
3. permitir tooltip com tipo, score e mensagem resumida.

No MVP, tooltip pode ser simplificado.

---

## 32. Acessibilidade mínima

O dashboard deve evitar depender exclusivamente de cor.

Cada alerta deverá ter texto claro.

Exemplo:

```text
ALERTA CRÍTICO
ALERTA DE ATENÇÃO
```

Não usar apenas cor vermelha/amarela.

---

## 33. Mensagens proibidas

A interface não deverá usar mensagens como:

1. comprar agora;
2. vender agora;
3. lucro garantido;
4. sinal infalível;
5. operação segura;
6. recomendação de investimento.

Mensagens permitidas:

1. possível rompimento;
2. alerta técnico;
3. acompanhar movimento;
4. perda de tendência;
5. dados insuficientes;
6. provider indisponível.

---

## 34. Critérios de aceite

## Cenário 1 — Dashboard online

Dado que existem candles válidos e análise disponível  
Quando o usuário abrir o dashboard  
Então a tela deverá exibir preço, tendência, EMAs, score e gráfico.

---

## Cenário 2 — Histórico insuficiente

Dado que não há candles suficientes  
Quando o usuário abrir o dashboard  
Então a tela deverá exibir o estado `WaitingForEnoughData`.

---

## Cenário 3 — Provider indisponível

Dado que o provider está indisponível  
Quando o usuário abrir o dashboard  
Então a tela deverá exibir o estado `ProviderUnavailable`.

---

## Cenário 4 — Dados obsoletos

Dado que o último candle fechado está atrasado  
Quando o usuário abrir o dashboard  
Então a tela deverá exibir o estado `StaleData`.

---

## Cenário 5 — Novo alerta

Dado que um novo alerta foi gerado  
Quando o dashboard atualizar  
Então o painel de alerta ativo deverá ser atualizado e o alerta deverá aparecer na lista de recentes.

---

## Cenário 6 — Som do alerta

Dado que `EnableSound` está habilitado  
E um novo alerta foi recebido  
Quando o dashboard atualizar  
Então a aplicação deverá emitir som uma única vez para esse alerta.

---

## Cenário 7 — Mesmo alerta repetido

Dado que o mesmo alerta continua sendo o último alerta  
Quando o dashboard atualizar novamente  
Então o som não deverá tocar novamente.

---

## Cenário 8 — Gráfico

Dado que existem pontos para exibição  
Quando o dashboard carregar  
Então o gráfico deverá exibir preço, linha de regressão e EMAs.

---

## 35. Testes mínimos esperados

## 35.1 DashboardAppService

Validar:

1. monta dashboard online;
2. monta dashboard com histórico insuficiente;
3. monta dashboard com provider indisponível;
4. monta dashboard com dados obsoletos;
5. retorna alertas recentes;
6. retorna pontos do gráfico.

---

## 35.2 Componentes Blazor

Validar, quando viável:

1. renderização dos cards;
2. renderização da lista de alertas;
3. exibição de mensagens de estado;
4. ausência de mensagens proibidas.

---

## 35.3 Alerta sonoro

Validar:

1. som toca para alerta novo;
2. som não repete para mesmo alerta;
3. som não toca quando desabilitado;
4. som não toca sem alerta.

---

## 36. Decisões importantes

1. O dashboard não terá regra técnica.
2. O dashboard consumirá `IDashboardAppService`.
3. O MVP usará gráfico de linha, não candlestick obrigatório.
4. O alerta sonoro será opcional.
5. O dashboard deverá tratar estados de falha.
6. O dashboard não exibirá recomendação direta de compra ou venda.
7. Timer simples é suficiente para o MVP.
8. SignalR fica como evolução futura.

---

## 37. Resultado esperado

Ao final desta SPEC, a aplicação deverá possuir um dashboard capaz de:

1. exibir status do mercado monitorado;
2. exibir preço, tendência e EMAs;
3. exibir gráfico com linha de tendência;
4. exibir alertas recentes;
5. destacar novo alerta;
6. emitir som de alerta;
7. representar estados de erro ou dados insuficientes;
8. apoiar decisão manual sem recomendar operação.
