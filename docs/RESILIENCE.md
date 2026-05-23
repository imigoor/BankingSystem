# Documentação de Resiliência — Polly

## 1. Endpoint Escolhido e Justificativa

### Endpoint: `GET /api/v1/clients/{id}` — chamado internamente pelo Microsserviço de Transações

O endpoint escolhido para aplicação das políticas de resiliência é a **validação de existência do cliente** (`ClientExistsAsync`), realizada pelo `ClientServiceClient` no Microsserviço de Transações toda vez que uma nova transferência é criada via `POST /api/v1/transfers`.

**Por que este endpoint?**

Este é o ponto de comunicação HTTP síncrona mais crítico do sistema. Antes de persistir qualquer transferência, o serviço de Transações precisa confirmar que ambos os usuários (remetente e destinatário) existem no Microsserviço de Clientes. Se essa chamada falhar sem resiliência, toda a criação de transferência falha — impactando diretamente a experiência do usuário e a integridade do negócio.

Diferente das comunicações via Azure Service Bus (que são assíncronas e tolerantes a falhas por natureza), esta chamada HTTP é **síncrona e bloqueante**, tornando-a o maior ponto de vulnerabilidade a instabilidades de rede, restart de containers, cold-start de pods em Kubernetes e picos de latência.

---

## 2. Estratégias de Resiliência Implementadas

As políticas são configuradas no `HttpClient` via `AddPolicyHandler()` no arquivo `Transactions.Infrastructure/DependencyInjection.cs`. Elas são aplicadas em camadas (wrap), na ordem: **Timeout → Circuit Breaker → Retry** (de dentro para fora na pilha de execução).

---

### 2.1 Retry com Backoff Exponencial

```csharp
HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
    );
```

| Parâmetro | Valor | Justificativa |
|---|---|---|
| `retryCount` | 3 | Três tentativas cobrem falhas transitórias sem expor o usuário a esperas longas. Com backoff exponencial, o tempo total máximo de espera é ~14s (2s + 4s + 8s). |
| `sleepDurationProvider` | `2^attempt` segundos | Backoff exponencial evita o "thundering herd" — múltiplos serviços reintentando simultaneamente sobrecarregariam o Clients service durante uma recuperação. |
| `HandleTransientHttpError` | 5xx + timeout de rede | Cobre os casos mais comuns de falha transitória: erros de servidor, DNS e timeouts de conexão. Erros 4xx (ex: 404) não são retentados, pois indicam problemas de negócio, não de infraestrutura. |

**Cenário coberto:** O pod do Clients service está reiniciando no Kubernetes. As primeiras 1-2 requisições falham com `503 Service Unavailable`, mas após ~6 segundos o pod está saudável e a terceira tentativa tem sucesso.

---

### 2.2 Circuit Breaker

```csharp
HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30)
    );
```

| Parâmetro | Valor | Justificativa |
|---|---|---|
| `handledEventsAllowedBeforeBreaking` | 5 | Cinco falhas consecutivas são uma evidência forte de que o serviço está indisponível, e não apenas sofrendo falhas transitórias pontuais. Um limiar menor (ex: 2) causaria abertura do circuito por problemas momentâneos. |
| `durationOfBreak` | 30 segundos | 30 segundos é tempo suficiente para um pod reiniciar ou um deploy ser concluído, sem bloquear o sistema por muito tempo. Após esse período, o circuito vai para **Half-Open** e testa uma requisição de sondagem. |

**Estados do circuito:**
- **Closed** (normal): todas as requisições passam.
- **Open** (falha detectada): requisições falham imediatamente com `BrokenCircuitException`, sem tentar conectar ao serviço. Protege os recursos de thread-pool.
- **Half-Open** (recuperação): uma requisição de teste é enviada. Se bem-sucedida, o circuito fecha; se falhar, volta a Open por mais 30s.

**Cenário coberto:** O banco de dados do Clients service ficou inacessível. Sem o Circuit Breaker, cada requisição de transferência esperaria o timeout (5s) × 3 retries = 15 segundos antes de falhar, esgotando rapidamente o pool de threads sob carga. Com o circuito aberto, as falhas acontecem instantaneamente.

---

### 2.3 Timeout por Requisição

```csharp
Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(5));
```

| Parâmetro | Valor | Justificativa |
|---|---|---|
| `timeout` | 5 segundos | Uma consulta de existência de cliente é uma operação simples (select por PK com cache Redis). Qualquer resposta acima de 5s indica um problema sério no Clients service. Esse limite protege o thread-pool do Transactions service de acumular requisições pendentes. |

> **Importante:** O Timeout é aplicado **por tentativa** (não ao total das retentativas). Com 3 retries, o tempo máximo total de espera é aproximadamente `3 × 5s + (2s + 4s + 8s)` = **29 segundos** no pior caso, antes que a criação de transferência falhe definitivamente.

---

## 3. Ordem de Composição das Políticas (Policy Wrap)

As políticas são registradas na seguinte ordem via `AddPolicyHandler`:

```
Requisição → [Retry] → [Circuit Breaker] → [Timeout] → HTTP call
```

Esta ordem é intencional:

1. **Timeout** é o mais interno: cada tentativa individual tem seu próprio timeout de 5s.
2. **Circuit Breaker** envolve o timeout: se o timeout disparar repetidamente, essas falhas contam para o contador do circuito.
3. **Retry** é o mais externo: ao receber uma falha (incluindo de timeout ou circuito aberto), ele decide se tenta novamente.

---

## 4. Diagrama de Comunicação entre Microsserviços

```
┌─────────────────────────────────────────────────────────────────┐
│                        Cliente HTTP / Browser                    │
└──────────────────────────┬──────────────────────────────────────┘
                           │ JWT Bearer Token
          ┌────────────────┼────────────────┐
          │                                 │
          ▼                                 ▼
┌──────────────────┐               ┌──────────────────┐
│  Transactions    │               │    Clients        │
│      API         │               │      API          │
│  :5001 / :8080   │               │  :5002 / :8080   │
│                  │               │                   │
│  POST /transfers │               │  GET /clients/{id}│
│  GET  /transfers │               │  PATCH /clients/  │
│  GET  /users/    │               │  PATCH .../photo  │
│       /transfers │               │                   │
└────────┬─────────┘               └────────┬──────────┘
         │                                  │
         │  HTTP Síncrono                   │  Escrita
         │  (Polly: Retry +                 │
         │   CircuitBreaker +               │
         │   Timeout)                       │
         │                                  │
         └──────────────►──────────────────►┘
                         Clients API
                         (valida sender/receiver)
         │                                  │
         │                                  │  Publica evento
         │                                  │  (banking data changed)
         │                                  ▼
         │                    ┌─────────────────────────┐
         │                    │   Azure Service Bus      │
         │                    │  Topic: banking-updates  │
         │                    └─────────────┬───────────┘
         │                                  │
         │  Consome evento                  │
         ◄──────────────────────────────────┘
    (ClientBankingDataUpdatedConsumer)
         │
         ▼
  ┌──────────────┐    ┌───────────────┐
  │  SQL Server  │    │  SQL Server   │
  │ TransactionsDB│   │  ClientsDB    │
  └──────────────┘    └──────┬────────┘
                             │ Cache-Aside
                             ▼
                      ┌─────────────┐
                      │    Redis    │
                      │  (30 min)   │
                      └─────────────┘
```

### Legenda de decisão: HTTP vs Mensageria

| Comunicação | Mecanismo | Motivo |
|---|---|---|
| Transactions → Clients (validação) | HTTP síncrono + Polly | A criação da transferência **depende** da confirmação de existência do cliente. Não é possível prosseguir sem a resposta. |
| Clients → Transactions (banking atualizado) | Azure Service Bus (async) | O serviço de Transações não precisa saber **imediatamente** que dados bancários mudaram. A mensagem pode ser processada com atraso sem impacto no negócio, garantindo desacoplamento. |

---

## 5. Observabilidade das Políticas

Todos os eventos das políticas (retry, circuit open/close, timeout) são logados via callbacks (`onRetry`, `onBreak`, `onReset`, `onTimeoutAsync`) e integrados ao Serilog com log estruturado, permitindo rastreamento completo em ferramentas como Azure Monitor, Application Insights ou Seq.
