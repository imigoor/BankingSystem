# 🏦 Banking System — Desafio .NET

Sistema bancário com dois microsserviços independentes desenvolvido como desafio técnico, utilizando **.NET 8**, **C# 12**, **Clean Architecture**, **CQRS/MediatR** e infraestrutura completa na **Azure**.

---

## 🔗 Links de Produção

| Serviço | URL |
|---|---|
| **Transactions API** | https://app-transactions-api-hnhqckd6abfaakdt.chilecentral-01.azurewebsites.net/index.html |
| **Clients API** | https://app-clients-api-etfgdmfhdedvbzh7.chilecentral-01.azurewebsites.net/index.html |

---

## 🏗️ Arquitetura

O sistema segue **Clean Architecture** estrita com 4 projetos separados por microsserviço:

```
BankingSystem/
├── src/
│   ├── Transactions/
│   │   ├── Transactions.API/            # Controllers, Middlewares, Swagger, JWT
│   │   ├── Transactions.Application/   # CQRS, MediatR, FluentValidation, Interfaces
│   │   ├── Transactions.Domain/         # Entidades, Value Objects, Exceções de Domínio
│   │   └── Transactions.Infrastructure/ # EF Core, Polly, MassTransit, Repositórios
│   └── Clients/
│       ├── Clients.API/                 # Controllers, Middlewares, Swagger, JWT
│       ├── Clients.Application/         # CQRS, MediatR, FluentValidation, Interfaces
│       ├── Clients.Domain/              # Entidades, Value Objects, Exceções de Domínio
│       └── Clients.Infrastructure/      # EF Core, Redis, Azure Blob, SendGrid, MassTransit
├── tests/
│   ├── Transactions.Tests/              # Testes unitários
│   └── Clients.Tests/                   # Testes unitários + integração
├── docs/
│   └── RESILIENCE.md                    # Documentação das políticas Polly
├── docker-compose.yml
├── .env.example
└── BankingSystem.sln
```

### Diagrama de Comunicação entre Microsserviços

```
┌──────────────────┐         HTTP Síncrono + Polly          ┌──────────────────┐
│  Transactions    │ ─────────────────────────────────────► │    Clients        │
│      API         │   GET /clients/{id}/exists              │      API          │
│                  │   (Retry 3x · CircuitBreaker · 30s)    │                  │
└────────┬─────────┘                                         └────────┬──────────┘
         │                                                            │
         │  EF Core                                         EF Core  │  Redis Cache
         ▼                                                            ▼
  ┌─────────────┐                                        ┌───────────────────────┐
  │  SQL Server  │                                        │  SQL Server + Redis   │
  │ TransactionsDB│                                       │     ClientsDB         │
  └─────────────┘                                         └───────────────────────┘
         │                                                            │
         │          Azure Service Bus (Assíncrono)                    │
         ◄────────────────────────────────────────────────────────────┘
    MassTransit Consumer                              MassTransit Publisher
    (ClientBankingDataUpdated)                  (PATCH /clients/{id} → banking)
```

**Decisão de design:** A validação de clientes no fluxo de criação de transferência usa HTTP síncrono com Polly porque a resposta é necessária em tempo real, não é possível criar uma transferência sem confirmar que os usuários existem. A notificação de mudança de dados bancários usa mensageria assíncrona via Azure Service Bus porque o Transactions service não precisa saber imediatamente da mudança, garantindo desacoplamento e resiliência.

---

## 📡 Endpoints

### Microsserviço de Transações (porta 5001 local / Azure)

| Método | Rota | Descrição | Role |
|--------|------|-----------|------|
| `POST` | `/api/v1/auth/login` | Gera JWT Bearer token | Público |
| `POST` | `/api/v1/transfers` | Cria nova transferência | User, Admin |
| `GET`  | `/api/v1/transfers/{id}` | Detalhes de uma transferência | User, Admin |
| `GET`  | `/api/v1/users/{userId}/transfers` | Transferências de um usuário | User, Admin |

### Microsserviço de Clientes (porta 5002 local / Azure)

| Método | Rota | Descrição | Role |
|--------|------|-----------|------|
| `POST` | `/api/v1/auth/login` | Gera JWT Bearer token | Público |
| `GET`  | `/api/v1/clients/{id}` | Detalhes do cliente (Redis → SQL) | User, Admin |
| `GET`  | `/api/v1/clients/{id}/exists` | Verificação interna (sem auth) | Público |
| `PATCH`| `/api/v1/clients/{id}` | Atualização parcial dos dados | User, Admin |
| `PATCH`| `/api/v1/clients/{id}/profile-picture` | Upload de foto (Azure Blob) | User, Admin |

---

## 🔐 Autenticação

Ambas as APIs utilizam **JWT Bearer** com **RBAC** (Role-Based Access Control).

```bash
curl -X POST https://app-transactions-api-hnhqckd6abfaakdt.chilecentral-01.azurewebsites.net/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "Admin@123"}'
```

| Username | Password | Role |
|----------|----------|------|
| `admin` | `Admin@123` | Admin |
| `user` | `User@123` | User |

Use o token retornado no header: `Authorization: Bearer <token>`

---

## 🛡️ Resiliência (Polly)

Aplicada na comunicação HTTP do Transactions → Clients service, com três políticas compostas:

| Política | Configuração | Justificativa |
|---|---|---|
| **Retry** | 3 tentativas, backoff exponencial (2s, 4s, 8s) | Falhas transitórias de rede e cold-start de containers |
| **Circuit Breaker** | Abre após 5 falhas consecutivas, fecha em 30s | Evita sobrecarga do Clients service durante indisponibilidade |
| **Timeout** | 5s por tentativa | Previne starvation de threads em chamadas pendentes |

Documentação completa em [`docs/RESILIENCE.md`](docs/RESILIENCE.md).

---

## 🗄️ Banco de Dados e Cache

- **SQL Server** (Azure SQL Serverless) — tabelas `Clients` e `Transfers` no mesmo banco
- **Redis** (Azure Cache for Redis) — cache de leitura de clientes com TTL de 30 minutos
- **Padrão Cache-Aside:** Redis → SQL (miss) → popula cache

Migrations aplicadas automaticamente via `db.Database.Migrate()` no startup.

---

## 📨 Mensageria

**Azure Service Bus + MassTransit**

Quando `PATCH /clients/{id}` altera dados bancários (`agency` ou `accountNumber`), o Clients service publica um evento `ClientBankingDataUpdatedEvent`. O Transactions service tem um `Consumer` registrado que processa esse evento de forma assíncrona.

---

## ☁️ Armazenamento em Nuvem

- **Azure Blob Storage** — upload de fotos de perfil via `PATCH /clients/{id}/profile-picture`
- **SendGrid** — envio de e-mail de confirmação ao atualizar dados do cliente

---

## 🧪 Testes

```bash
dotnet test
```

| Projeto | Tipo | Cobertura |
|---|---|---|
| `Transactions.Tests` | Unitários | `CreateTransferCommandHandler` — criação, validações, casos de erro |
| `Clients.Tests` | Unitários | `GetClientQueryHandler` — cache hit, cache miss, not found |
| `Clients.Tests` | Integração | `ClientsApiIntegrationTests` — endpoints via WebApplicationFactory |

---

## 🐳 Docker (ambiente local)

```bash
# Configure as variáveis
cp .env.example .env
# Edite o .env com suas credenciais

# Suba todos os serviços
docker compose up --build

# Swagger local
# Transactions: http://localhost:5001/swagger
# Clients:      http://localhost:5002
```

O `docker-compose.yml` orquestra: SQL Server, Redis, Transactions API e Clients API.

---

## 🚀 CI/CD — GitHub Actions

Duas pipelines independentes em `.github/workflows/`:

| Pipeline | Trigger | Destino |
|---|---|---|
| `deploy-clients.yml` | Push em `main` com mudanças em `src/Clients/**` | Azure App Service `app-clients-api` |
| `deploy-transactions.yml` | Push em `main` com mudanças em `src/Transactions/**` | Azure App Service `app-transactions-api` |

---

## 🌿 Gitflow

| Branch | Propósito |
|---|---|
| `main` | Produção, código deployado na Azure |
| `test` | Branch para validação de testes e experimentos |
| `develop` | Integração de features |
| `feature/*` | Novas funcionalidades |
| `fix/*` | Correções de bugs |
| `bugfix/*` | Ajustes de comportamento |
| `refactor/*` | Refatorações sem mudança funcional |

---

## ⚙️ Configuração de Ambiente

Copie `.env.example` para `.env` e preencha:

```env
SQL_SERVER_PASSWORD=SuaSenhaForte!
REDIS_PASSWORD=SuaSenhaRedis
JWT_SECRET=sua-chave-secreta-com-32-chars-minimo
AZURE_SERVICE_BUS_CONNECTION_STRING=Endpoint=sb://...
AZURE_BLOB_CONNECTION_STRING=DefaultEndpointsProtocol=https;...
SENDGRID_API_KEY=SG.sua_chave
SENDGRID_FROM_EMAIL=noreply@seudominio.com
CLIENTS_SERVICE_BASE_URL=http://localhost:5002
```

**Nenhuma credencial está hardcoded no código**, tudo lido via `IConfiguration` do .NET.

---

## 🛠️ Stack Tecnológica

| Camada | Tecnologia |
|--------|-----------|
| Framework | .NET 8 / C# 12 |
| Arquitetura | Clean Architecture + CQRS |
| Mediator | MediatR 12 |
| Validação | FluentValidation 11 |
| ORM | Entity Framework Core 8 (SQL Server) |
| Cache | Redis (StackExchange.Redis) |
| Mensageria | MassTransit 8 + Azure Service Bus |
| Resiliência | Polly 8 (Retry, Circuit Breaker, Timeout) |
| Blob Storage | Azure.Storage.Blobs |
| Email | SendGrid |
| Auth | JWT Bearer + RBAC |
| Logging | Serilog (Console + File, logs estruturados) |
| Docs | Swagger / OpenAPI (Swashbuckle) |
| Testes | xUnit + NSubstitute + FluentAssertions |
| Containers | Docker + Docker Compose |
| CI/CD | GitHub Actions |
| Cloud | Azure App Service + Azure SQL + Azure Cache for Redis + Azure Blob Storage |

---

## 📋 Pré-requisitos para rodar localmente

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- Variáveis de ambiente configuradas (`.env`)

---

## 👤 Autor

Igor Miranda · Maio 2026
