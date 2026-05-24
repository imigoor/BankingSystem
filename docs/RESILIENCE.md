# Documentação de Resiliência — Polly

## 1. Endpoint Escolhido e Justificativa

### Endpoint: `GET /api/v1/clients/{id}` — chamado internamente pelo Microsserviço de Transações

O endpoint escolhido para aplicação das políticas de resiliência é a **validação de existência do cliente** (`ClientExistsAsync`), realizada pelo `ClientServiceClient` no Microsserviço de Transações toda vez que uma nova transferência é criada via `POST /api/v1/transfers`.

O fluxo concreto é:

1. O usuário chama `POST /api/v1/transfers`
2. O `CreateTransferCommandHandler` chama `ClientExistsAsync(senderUserId)` e `ClientExistsAsync(receiverUserId)`
3. Essas chamadas fazem um `GET /api/v1/clients/{id}` no Clients service via `HttpClient`
4. O `TokenForwardingHandler` injeta automaticamente o JWT Bearer do request original em cada chamada
5. Só após confirmação de ambos os clientes a transferência é persistida no banco

**Por que este endpoint?**

Este é o ponto de comunicação bloqueante mais crítico do sistema. O fluxo de criação de transferência **não pode avançar** sem receber a resposta do Clients service, há dependência sequencial obrigatória entre os dois serviços. Se essa chamada falhar sem resiliência, toda a criação de transferência falha, impactando diretamente a experiência do usuário e a integridade do negócio.

> **Nota sobre async/await:** O método `ClientExistsAsync` usa `await` internamente, o que libera a thread enquanto aguarda a resposta (I/O não bloqueante). Isso é uma característica de implementação. A comunicação ainda é **bloqueante do ponto de vista de negócio**, o handler não avança sem a resposta, ao contrário da mensageria com Azure Service Bus onde o fluxo continua independentemente.

Diferente das comunicações via Azure Service Bus (assíncronas e tolerantes a falhas por natureza), esta chamada HTTP é sequencialmente bloqueante, tornando-a o maior ponto de vulnerabilidade a instabilidades de rede, restart de containers e picos de latência justamente onde o Polly agrega mais valor.

---

## 2. Estratégias de Resiliência Implementadas

As políticas são configuradas no `HttpClient` via `AddPolicyHandler()` em `Transactions.Infrastructure/DependencyInjection.cs`. São aplicadas em camadas (policy wrap) na seguinte ordem de registro:

```csharp
.AddHttpMessageHandler<TokenForwardingHandler>()
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy())
.AddPolicyHandler(GetTimeoutPolicy())
```

---

### 2.1 Retry com Backoff Exponencial

```csharp
private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    => HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryAttempt, _) =>
            {
                Console.WriteLine(
                    $"[Polly Retry] Attempt {retryAttempt} after {timespan.TotalSeconds}s due to: " +
                    $"{outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
            });
```

| Parâmetro | Valor | Justificativa |
|---|---|---|
| `retryCount` | 3 | Três tentativas cobrem a maioria das falhas transitórias sem deixar o usuário esperando indefinidamente. |
| `sleepDurationProvider` | `2^attempt` segundos (2s, 4s, 8s) | Backoff exponencial evita o "thundering herd" múltiplos clientes reintentando simultaneamente após uma falha sobrecarregariam o Clients service durante sua recuperação. |
| `HandleTransientHttpError` | Erros 5xx + falhas de rede | Cobre erros transitórios de servidor e DNS. Erros 4xx (ex: 404 Not Found) **não são retentados**, pois indicam problemas de negócio determinísticos, não falhas de infraestrutura. |

**Observabilidade:** Cada tentativa loga via `Console.WriteLine` o número da tentativa, o tempo de espera e o motivo da falha, visível no Log Stream do Azure App Service.

**Cenário coberto:** O Clients service está reiniciando após um redeploy. As primeiras requisições falham com `503 Service Unavailable`, mas após ~6 segundos o serviço está saudável e a terceira tentativa tem sucesso sem que o usuário perceba a falha.

---

### 2.2 Circuit Breaker

```csharp
private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    => HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (_, duration) =>
                Console.WriteLine($"[Polly CircuitBreaker] Circuit OPEN for {duration.TotalSeconds}s"),
            onReset: () =>
                Console.WriteLine("[Polly CircuitBreaker] Circuit CLOSED — service recovered"),
            onHalfOpen: () =>
                Console.WriteLine("[Polly CircuitBreaker] Circuit HALF-OPEN — testing recovery"));
```

| Parâmetro | Valor | Justificativa |
|---|---|---|
| `handledEventsAllowedBeforeBreaking` | 5 | Cinco falhas consecutivas são evidência forte de indisponibilidade real, não de falhas pontuais. Um limiar menor (ex: 2) abriria o circuito por problemas momentâneos, causando falhas desnecessárias. |
| `durationOfBreak` | 30 segundos | Tempo suficiente para um pod reiniciar ou um deploy ser concluído. Após esse período o circuito entra em Half-Open e testa uma requisição de sondagem. |

**Estados do circuito:**
- **Closed** (normal): todas as requisições passam normalmente.
- **Open** (falha detectada): requisições falham imediatamente com `BrokenCircuitException`, sem nem tentar conectar ao Clients service. Protege o thread-pool do Transactions service.
- **Half-Open** (recuperação): uma requisição de teste é enviada. Se bem-sucedida, o circuito fecha (Closed); se falhar, volta a Open por mais 30s.

**Observabilidade:** Transições de estado são logadas via callbacks `onBreak`, `onReset` e `onHalfOpen`, visíveis no Log Stream do Azure.

**Cenário coberto:** O banco de dados do Clients service ficou inacessível. Sem o Circuit Breaker, cada requisição de transferência aguardaria o timeout (60s) antes de falhar, esgotando threads sob carga. Com o circuito aberto, as falhas são imediatas e o sistema se recupera sozinho após 30 segundos.

---

### 2.3 Timeout por Tentativa

```csharp
private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    => Policy.TimeoutAsync<HttpResponseMessage>(
        TimeSpan.FromSeconds(60),
        onTimeoutAsync: (_, timeout, _, _) =>
        {
            Console.WriteLine($"[Polly Timeout] Request timed out after {timeout.TotalSeconds}s");
            return Task.CompletedTask;
        });
```

| Parâmetro | Valor | Justificativa |
|---|---|---|
| `timeout` | 60 segundos | Configurado para acomodar a latência do **Azure SQL Serverless**, que pode hibernar após períodos de inatividade e levar até 30-40s para reativar na primeira conexão. Em produção com banco sempre ativo (`Always On`), esse valor pode ser reduzido para 10-15s. |

> **Importante:** O timeout é aplicado **por tentativa individual**, não ao total das retentativas. No pior caso com 3 retries completos: `3 × 60s + (2s + 4s + 8s)` = **194 segundos** antes de falhar definitivamente. Na prática, a maioria das falhas resolve na primeira ou segunda tentativa.

**Observabilidade:** Timeouts são logados via callback `onTimeoutAsync`, visíveis no Log Stream do Azure.

---

## 3. Ordem de Composição das Políticas (Policy Wrap)

O Polly aplica as políticas de fora para dentro na ordem de registro via `AddPolicyHandler`. Como o Retry foi registrado primeiro, ele é o mais externo:

```
Requisição de entrada
    → [TokenForwardingHandler] injeta JWT
    → [Retry] decide se tenta novamente após falha
        → [Circuit Breaker] bloqueia se circuito aberto
            → [Timeout 60s] limita cada tentativa individual
                → HTTP call para o Clients service
```

Esta ordem é intencional:

1. **Timeout (mais interno):** cada tentativa individual tem no máximo 60s. Se estourar, lança `TimeoutRejectedException`.
2. **Circuit Breaker (meio):** conta as falhas (incluindo timeouts). Após 5 falhas consecutivas, abre o circuito e passa a rejeitar imediatamente por 30s.
3. **Retry (mais externo):** ao receber qualquer falha (timeout, circuito aberto, erro HTTP), decide se faz mais uma tentativa com backoff exponencial.

---

## 4. Diagrama de Comunicação entre Microsserviços

```
┌──────────────────────────────────────────────────────────────────┐
│                     Cliente HTTP / Swagger                        │
└──────────────────────────┬───────────────────────────────────────┘
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
│       /transfers │               │  GET  .../exists  │
└────────┬─────────┘               └────────┬──────────┘
         │                                  │
         │  HTTP Bloqueante                 │  Escrita no banco
         │  TokenForwardingHandler          │  + Cache Redis
         │  + Polly:                        │
         │    Retry (3x, exp backoff)       │
         │    CircuitBreaker (5 falhas/30s) │
         │    Timeout (60s/tentativa)       │
         │                                  │
         └──────────────► GET /clients/{id} ┘
                         (valida sender e receiver)
         │                                  │
         │                                  │  Publica evento quando
         │                                  │  banking data muda
         │                                  ▼
         │                    ┌─────────────────────────┐
         │                    │   Azure Service Bus      │
         │                    │  (MassTransit Publisher) │
         │                    └─────────────┬───────────┘
         │                                  │
         │  Consome evento assíncrono       │
         ◄──────────────────────────────────┘
    (ClientBankingDataUpdatedConsumer)
         │
         ▼
  ┌──────────────┐         ┌──────────────┐
  │  SQL Server  │         │  SQL Server  │
  │ TransactionsDB│        │  ClientsDB   │
  └──────────────┘         └──────┬───────┘
                                  │ Cache-Aside
                                  ▼
                           ┌─────────────┐
                           │    Redis    │
                           │  TTL 30min  │
                           └─────────────┘
```

### Decisão: HTTP síncrono vs Mensageria assíncrona

| Comunicação | Mecanismo | Motivo |
|---|---|---|
| Transactions → Clients (validação) | HTTP bloqueante + Polly | O handler **depende** da confirmação de existência do cliente para prosseguir. Não é possível criar uma transferência sem essa resposta — há dependência sequencial de negócio. |
| Clients → Transactions (banking atualizado) | Azure Service Bus + MassTransit | O Transactions service não precisa saber **imediatamente** que dados bancários mudaram. O evento pode ser processado com atraso sem impacto no negócio, garantindo desacoplamento total entre os serviços. |

---

## 5. Observabilidade

Todos os eventos das políticas são logados via callbacks e integrados ao **Serilog** com log estruturado. No Azure, os logs aparecem em:

- **Log Stream** do App Service em tempo real
- **Application Insights** (se configurado)

Exemplo de sequência de logs durante uma falha e recuperação:

```
[Polly Retry] Attempt 1 after 2s due to: Connection refused
[Polly Retry] Attempt 2 after 4s due to: Connection refused
[Polly Retry] Attempt 3 after 8s due to: Connection refused
[Polly CircuitBreaker] Circuit OPEN for 30s
[Polly CircuitBreaker] Circuit HALF-OPEN — testing recovery
[Polly CircuitBreaker] Circuit CLOSED — service recovered
```
