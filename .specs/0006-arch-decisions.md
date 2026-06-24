# SPEC 0006 — Decisões Arquiteturais Complementares

## 1. Objetivo

Registrar decisões arquiteturais tomadas após a revisão das SPECs 0001 a 0005,
consolidando ajustes de comportamento, configuração e UX que não estavam
cobertos nas specs anteriores.

---

## 2. Escopo

Esta SPEC cobre:

1. Remoção do backfill inicial.
2. Configuração de provider via appSettings.
3. Comportamento durante mercado fechado.
4. Barra de progresso durante aquecimento orgânico.
5. Cooldown de alertas consecutivos.

---

## 3. Fora do escopo

Esta SPEC não define:

1. Como calcular o score de alerta.
2. Como buscar candles nos providers.
3. Como validar qualidade dos candles.
4. Como calcular regressão linear ou EMAs.

Esses pontos estão cobertos nas SPECs 0002, 0003 e 0004.

---

## 4. Remoção do backfill inicial

As seções 14 e 15 da SPEC 0003 estão revogadas.

A aplicação não executa backfill inicial.

O histórico é construído organicamente a partir da primeira execução.

Razão:

1. Providers gratuitos têm limites de requisição que inviabilizam
   backfill de 30 dias em M1.
2. Dados históricos intraday de providers gratuitos têm qualidade
   inconsistente.
3. A SPEC 0003 já prevê o estado WaitingForEnoughData para cobrir
   o período de aquecimento.

Regra:

```text
A análise técnica permanece bloqueada até que o sistema atinja
MinimumRequiredCandles. Após esse threshold, destrava automaticamente
sem necessidade de intervenção manual.
```

---

## 5. Configuração de provider

A troca de provider é uma decisão de infraestrutura, não de produto.

Não haverá interface no dashboard para trocar o provider ativo.

A configuração é feita exclusivamente via appSettings.json:

```json
"MarketDataProvider": {
  "Primary": "TwelveData",
  "Comparison": "AlphaVantage",
  "ActiveProvider": "TwelveData",
  "PollingIntervalSeconds": 120
}
```

Regras:

1. O provider ativo é definido pelo campo ActiveProvider.
2. O provider de comparação é opcional e desligado por padrão.
3. A troca de provider requer redeploy ou restart da aplicação.
4. O valor mínimo recomendado para PollingIntervalSeconds no
   free tier do Twelve Data é 120 segundos. Abaixo disso o limite
   diário de 800 requisições pode ser estourado.

Razão para não expor no dashboard:

1. Trocar de provider com histórico acumulado gera risco de
   inconsistência na base — o mesmo OpenTime pode ter preços
   ligeiramente diferentes entre providers.
2. A decisão de troca deve ser consciente e acompanhada de
   avaliação da base existente.

---

## 6. Comportamento durante mercado fechado

O XAUUSD opera aproximadamente 24 horas por dia, 5 dias por semana.
O mercado fecha na sexta-feira à noite e reabre no domingo à noite
(horário UTC).

Quando o provider retornar resposta vazia fora do horário de mercado:

1. O worker registra o evento em log como MarketClosed.
2. Nenhuma ação adicional é executada.
3. Nenhum alerta é gerado.
4. O ciclo de polling continua normalmente.
5. O dashboard exibe o status MarketClosed enquanto essa condição
   persistir.

Regra:

```text
Resposta vazia do provider fora do horário de mercado
é tratada como condição normal, não como falha.
```

---

## 7. Fuso horário no dashboard

Os horários exibidos no dashboard seguem o fuso UTC, conforme
retornado pelo provider.

Regra:

```text
Nenhuma conversão de fuso é aplicada pela aplicação.
A conversão para horário local fica a cargo do usuário.
```

Uma nota informativa deverá ser exibida no dashboard:

```text
Horários exibidos em UTC.
```

---

## 8. Barra de progresso durante WaitingForEnoughData

Enquanto o sistema estiver em estado WaitingForEnoughData, o dashboard
deverá exibir uma barra de progresso indicando a evolução da coleta.

Comportamento:

1. Exibir a barra com o progresso atual: N de MinimumRequiredCandles.
2. Exibir o texto: "Aguardando dados suficientes para análise."
3. Não exibir gráficos, scores ou alertas nesse estado.
4. Quando atingir MinimumRequiredCandles, a barra desaparece e
   o dashboard carrega normalmente, sem intervenção manual.

Cálculo do progresso:

```text
Progresso = (CandlesColetados / MinimumRequiredCandles) * 100
```

Exemplo:

```text
CandlesColetados = 87
MinimumRequiredCandles = 140
Progresso = 62%

Exibição: "87 de 140 candles coletados (62%)"
```

---

## 9. Cooldown de alertas consecutivos

Para evitar flooding de alertas quando o mercado permanece em
rompimento por períodos prolongados, o sistema limita alertas
consecutivos do mesmo tipo para o mesmo símbolo.

### 9.1 Comportamento

1. O sistema contabiliza alertas consecutivos do mesmo tipo
   para o mesmo símbolo.
2. O contador é exibido no dashboard como N/MaxConsecutiveAlerts.
3. Ao atingir MaxConsecutiveAlerts, novos alertas do mesmo tipo
   ficam bloqueados.
4. O contador reseta quando o score cair abaixo do threshold
   por pelo menos MinCyclesToReset ciclos consecutivos.
5. Após o reset, o sistema volta a alertar normalmente.

### 9.2 Razão para reset por ciclos e não por tempo fixo

Reset por tempo fixo bloquearia alertas válidos em cenários de
rompimento legítimo prolongado. Reset por ciclos consecutivos
abaixo do threshold respeita o comportamento real do mercado —
o sinal precisa efetivamente enfraquecer antes de o sistema
voltar a alertar.

### 9.3 Configuração

```json
"AlertCooldown": {
  "MaxConsecutiveAlerts": 5,
  "MinCyclesToReset": 3
}
```

### 9.4 Exemplo

```text
Ciclo 1: score alto → alerta 1/5
Ciclo 2: score alto → alerta 2/5
Ciclo 3: score alto → alerta 3/5
Ciclo 4: score alto → alerta 4/5
Ciclo 5: score alto → alerta 5/5
Ciclo 6: score alto → bloqueado (limite atingido)
Ciclo 7: score baixo → contador de reset: 1/3
Ciclo 8: score baixo → contador de reset: 2/3
Ciclo 9: score baixo → contador de reset: 3/3 → reset
Ciclo 10: score alto → alerta 1/5 (reiniciado)
```

---

## 10. Configuração consolidada

Adicionar ao appSettings.json:

```json
{
  "MarketDataProvider": {
    "Primary": "TwelveData",
    "Comparison": "AlphaVantage",
    "ActiveProvider": "TwelveData",
    "PollingIntervalSeconds": 120
  },
  "AlertCooldown": {
    "MaxConsecutiveAlerts": 5,
    "MinCyclesToReset": 3
  }
}
```

---

## 11. Decisões importantes

1. Backfill inicial foi removido. O histórico é orgânico.
2. Provider é configuração de infraestrutura, não de produto.
3. Mercado fechado é condição normal, não falha.
4. Horários são exibidos em UTC sem conversão.
5. WaitingForEnoughData exibe barra de progresso, não erro.
6. Cooldown de alertas reseta por ciclos abaixo do threshold,
   não por tempo fixo.

---

## 12. SPECs afetadas

| SPEC  | Seção         | Impacto                                      |
|-------|---------------|----------------------------------------------|
| 0003  | 14 e 15       | Revogadas. Backfill substituído por seção 4. |
| 0005  | Dashboard     | Adicionar barra de progresso e cooldown.     |
| 0002  | PollingInterval | Documentar valor mínimo recomendado.       |

---

## 13. Resultado esperado

Ao final desta SPEC, a aplicação deverá:

1. Iniciar sem backfill e aguardar dados organicamente.
2. Operar com provider definido via configuração.
3. Tratar mercado fechado sem gerar erros ou alertas.
4. Exibir progresso claro durante o aquecimento inicial.
5. Controlar flooding de alertas com cooldown inteligente.