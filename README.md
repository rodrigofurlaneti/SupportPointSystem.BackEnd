# FSI.SupportPointSystem.BackEnd — Reengenharia v2.0

> **Reengenharia total** do sistema de gestão de check-in/check-out de vendedores.  
> Stack: **.NET 9 / C# 13 · Clean Architecture · DDD · CQRS · MediatR · EF Core 9 · JWT**

---

## Sumário

1. [Visão Geral e Decisões Arquiteturais](#1-visão-geral-e-decisões-arquiteturais)
2. [Estrutura da Solução](#2-estrutura-da-solução)
3. [Diagrama de Camadas (Dependency Rule)](#3-diagrama-de-camadas-dependency-rule)
4. [Domain Layer — O Coração do Sistema](#4-domain-layer--o-coração-do-sistema)
5. [Application Layer — CQRS com MediatR](#5-application-layer--cqrs-com-mediatr)
6. [Infrastructure Layer](#6-infrastructure-layer)
7. [API Layer](#7-api-layer)
8. [Regras de Negócio Documentadas](#8-regras-de-negócio-documentadas)
9. [Testes](#9-testes)
10. [Executando o Projeto](#10-executando-o-projeto)
11. [Migrations e Banco de Dados](#11-migrations-e-banco-de-dados)
12. [Endpoints da API](#12-endpoints-da-api)
13. [Diferenças em Relação à v1](#13-diferenças-em-relação-à-v1)

---

## 1. Visão Geral e Decisões Arquiteturais

### Por que Clean Architecture + DDD?

O sistema v1 funcionava com **Application Services anêmicos** que continham toda a lógica de negócio, com entidades sem comportamento (plain DTOs). A reengenharia inverte isso:

| Aspecto | v1 (original) | v2 (reengenharia) |
|---|---|---|
| Lógica de negócio | Application Services | **Entidades/Agregados de Domínio** |
| Comunicação interna | Interfaces de AppService | **MediatR Commands/Queries** |
| Resposta de erro | Exceções de controle de fluxo | **Result Pattern** |
| Validação | FluentValidation nos AppServices | **Pipeline Behavior do MediatR** |
| Coordenadas (distância) | Cálculo no AppService | **Encapsulado no ValueObject** |
| Testes | Sem testes | **xUnit + Moq + FluentAssertions + BDD** |

### Princípios aplicados

- **Domain-Driven Design**: Agregados, Value Objects, Domain Events, Linguagem Ubíqua
- **SOLID**: SRP, OCP, LSP, ISP e DIP aplicados em todas as camadas
- **Object Calisthenics**: Construtores privados, fábricas estáticas, sem else, encapsulamento forte
- **Result Pattern**: Sem exceções para controle de fluxo esperado (erros de negócio = Result.Failure)

---

## 2. Estrutura da Solução

```
FSI.SupportPointSystem.sln
│
├── src/
│   ├── FSI.SupportPointSystem.Domain/          ← Zero dependências externas
│   │   ├── Common/                             Entity, ValueObject, IDomainEvent
│   │   ├── Entities/                           User, Seller, Customer, Visit
│   │   ├── ValueObjects/                       Coordinates, Cpf, Cnpj, Address
│   │   ├── Events/                             DomainEvents (records imutáveis)
│   │   ├── Exceptions/                         DomainException, BusinessRuleException...
│   │   └── Interfaces/                         IRepository<T>, IUnitOfWork, IPasswordHasher...
│   │
│   ├── FSI.SupportPointSystem.Application/     ← Depende apenas do Domain
│   │   ├── Common/
│   │   │   ├── Behaviors/                      ValidationBehavior, LoggingBehavior, DomainEventDispatch
│   │   │   └── Results/                        Result<T>, Error (Result Pattern)
│   │   ├── Features/
│   │   │   ├── Auth/Commands/Login/            LoginCommand + Handler + Validator
│   │   │   ├── Sellers/Commands/CreateSeller/  CreateSellerCommand + Handler + Validator
│   │   │   ├── Customers/Commands/Upsert/      UpsertCustomerCommand + Handler + Validator
│   │   │   └── Visits/
│   │   │       ├── Commands/RegisterCheckin/   RegisterCheckinCommand + Handler + Validator
│   │   │       ├── Commands/RegisterCheckout/  RegisterCheckoutCommand + Handler + Validator
│   │   │       └── Queries/GetVisitHistory/    GetVisitHistoryQuery + Handler
│   │   └── DependencyInjection.cs
│   │
│   ├── FSI.SupportPointSystem.Infrastructure/  ← Depende de Application + Domain
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs                 EF Core Code First + Domain Event collector
│   │   │   ├── Configurations/                 IEntityTypeConfiguration<T> por entidade
│   │   │   ├── Repositories/                   Implementações concretas
│   │   │   └── UnitOfWork.cs                   + EfDomainEventCollector
│   │   ├── Services/                           BcryptPasswordHasher, JwtTokenService
│   │   └── DependencyInjection.cs
│   │
│   └── FSI.SupportPointSystem.Api/             ← Ponto de entrada HTTP
│       ├── Controllers/                        AuthController, VisitController, SellerController...
│       ├── Middleware/                         GlobalExceptionHandlerMiddleware
│       └── Program.cs
│
├── tests/
│   ├── FSI.SupportPointSystem.Domain.Tests/    xUnit — Entidades, ValueObjects
│   ├── FSI.SupportPointSystem.Application.Tests/  xUnit + Moq — Handlers
│   └── FSI.SupportPointSystem.Integration.Tests/  WebApplicationFactory + InMemory
│
└── features/
    └── gestao_checkin_checkout.feature         BDD Gherkin — 20+ cenários
```

---

## 3. Diagrama de Camadas (Dependency Rule)

```
┌─────────────────────────────────────────────┐
│                   API Layer                 │
│  Controllers · Middleware · Program.cs      │
│  (depende de Application + Infrastructure) │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│             Application Layer               │
│  Commands · Queries · Handlers · Behaviors  │
│           (depende apenas do Domain)        │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│               Domain Layer                  │
│  Entities · ValueObjects · Events           │
│  (ZERO dependências externas - puro C#)     │
└─────────────────────────────────────────────┘
                  ▲
┌─────────────────┴───────────────────────────┐
│            Infrastructure Layer             │
│  EF Core · Repositories · JWT · BCrypt      │
│   (implementa interfaces do Domain)         │
└─────────────────────────────────────────────┘
```

A seta da Infrastructure aponta **para cima** (para Domain/Application) — a regra de dependência do Clean Architecture é respeitada: código de alto nível não conhece implementações de baixo nível.

---

## 4. Domain Layer — O Coração do Sistema

### Agregados e suas invariantes

#### `Visit` — Agregado raiz principal

```csharp
// A entidade Visit protege suas próprias invariantes
var visit = Visit.RegisterCheckin(sellerId, customer, sellerLocation, hasActiveVisit);
// ↑ Lança BusinessRuleException("OutsideCheckinRadius") se distância > 100m
// ↑ Lança BusinessRuleException("MultipleCheckinBlocked") se já há visita aberta

visit.RegisterCheckout(sellerLocation, customer, summary);
// ↑ Lança BusinessRuleException("OutsideCheckinRadius") se distância > 100m
// ↑ Lança BusinessRuleException("VisitAlreadyClosed") se já foi fechada
```

#### `Coordinates` — Value Object com Haversine embutido

```csharp
var target = Coordinates.Create(-23.550520m, -46.633308m);
var seller = Coordinates.Create(-23.550600m, -46.633400m);

double meters = seller.DistanceInMetersTo(target);     // ~50m
bool isOk = seller.IsWithinRadiusOf(target, 100.0);   // true
```

#### `Cpf` / `Cnpj` — Value Objects com algoritmo de validação

```csharp
var cpf = Cpf.Create("529.982.247-25");  // Valida dígitos verificadores
cpf.Formatted;  // "529.982.247-25"
cpf.Value;      // "52998224725"
```

### Domain Events

Todos os eventos são **records imutáveis** que implementam `IDomainEvent`:

| Evento | Disparado quando |
|---|---|
| `CheckinRegisteredDomainEvent` | Vendedor realiza check-in com sucesso |
| `CheckoutRegisteredDomainEvent` | Vendedor realiza check-out com sucesso |
| `SellerCreatedDomainEvent` | Novo vendedor é cadastrado |
| `CustomerUpsertedDomainEvent` | Cliente é criado ou atualizado |

Os eventos são coletados do `ChangeTracker` do EF Core e publicados via MediatR **após o commit**, garantindo consistência.

---

## 5. Application Layer — CQRS com MediatR

### Pipeline de Behaviors (ordem de execução)

```
Request → LoggingBehavior → ValidationBehavior → DomainEventDispatchBehavior → Handler
```

1. **LoggingBehavior**: Loga início, fim e tempo de execução de cada Command/Query
2. **ValidationBehavior**: Executa todos os `IValidator<TRequest>` registrados; retorna 422 se inválido
3. **DomainEventDispatchBehavior**: Publica Domain Events coletados após o Handler concluir
4. **Handler**: Executa a lógica de aplicação e retorna `Result<T>`

### Result Pattern

```csharp
// No Handler:
return Result<CheckinResponse>.Failure(Error.Custom("OUTSIDE_RADIUS", "Fora do raio..."));
return Result<CheckinResponse>.Success(new CheckinResponse(...));

// No Controller:
return result.Match<IActionResult>(
    onSuccess: response => CreatedAtAction(..., response),
    onFailure: error => error.Code switch
    {
        "OUTSIDE_RADIUS"    => StatusCode(403, ...),
        "CONFLICT_CHECKIN"  => Conflict(...),
        _                   => BadRequest(...)
    });
```

---

## 6. Infrastructure Layer

### EF Core — Code First

Todas as configurações de mapeamento usam `IEntityTypeConfiguration<T>`:

- **Value Objects** mapeados como `OwnsOne()` (sem tabela separada)
- **Coordenadas** armazenadas com `HasPrecision(12, 9)` — 9 casas decimais (~1cm de precisão)
- **Índice filtrado** em `Visits(SellerId)` onde `CheckoutTimestamp IS NULL` — busca de visita ativa O(log n)
- **Enum `UserRole`** convertido para string no banco (`HasConversion<string>()`)

### Segurança

- **BCrypt** com work factor 12 (BcryptPasswordHasher)
- **JWT** com HMAC-SHA256, expiração de 8h, sem margem de clock skew
- Claims no token: `sub` (UserId), `role`, `cpf`, `sellerId` (se vendedor), `sellerName`

---

## 7. API Layer

### Middleware de Exceções

O `GlobalExceptionHandlerMiddleware` captura exceções e as converte em respostas padronizadas:

| Exceção | HTTP Status | Código |
|---|---|---|
| `ValidationException` | 422 | `VALIDATION_FAILED` |
| `NotFoundException` | 404 | `NOT_FOUND` |
| `BusinessRuleException` | 422 | `{RuleName}` |
| `DomainValidationException` | 400 | `DOMAIN_VALIDATION` |
| `Exception` (inesperada) | 500 | `INTERNAL_ERROR` |

### Autorização por Role

| Endpoint | Role necessária |
|---|---|
| `POST /api/auth/login` | Pública |
| `POST /api/sellers` | ADMIN |
| `POST /api/customers` | ADMIN |
| `POST /api/visits/checkin` | SELLER |
| `POST /api/visits/checkout` | SELLER |
| `GET /api/visits/history` | SELLER, ADMIN |

---

## 8. Regras de Negócio Documentadas

### RN-01: Raio de Check-in/Check-out
- O vendedor deve estar a **no máximo 100 metros** do ponto alvo do cliente para realizar check-in ou check-out.
- Distância calculada pela **fórmula de Haversine** sobre coordenadas WGS-84.
- Violação retorna **HTTP 403** com código `OUTSIDE_RADIUS`.

### RN-02: Visita Única por Vendedor
- Um vendedor **não pode ter dois check-ins abertos** simultaneamente.
- Tentativa retorna **HTTP 409** com código `CONFLICT_CHECKIN`.

### RN-03: Sequência Check-in → Check-out
- Check-out **exige check-in ativo** do mesmo vendedor.
- Sem visita ativa retorna **HTTP 400** com código `NO_ACTIVE_VISIT`.

### RN-04: Cálculo Automático de Duração
- `DurationMinutes = CheckoutTimestamp - CheckinTimestamp` (inteiro, arredondado para baixo).

### RN-05: Unicidade de CPF
- O CPF é a chave natural do `User`. Tentativa de duplicata retorna **HTTP 409** com `CPF_ALREADY_EXISTS`.
- Validação completa do algoritmo dos dígitos verificadores no Value Object `Cpf`.

### RN-06: Unicidade de CNPJ (Upsert)
- Se CNPJ já existe, **atualiza** o cliente. Caso contrário, **cria** novo.

### RN-07: Vendedor Inativo
- Vendedor com `IsActive = false` não consegue autenticar — retorna **HTTP 401**.

### RN-08: Expiração do Token JWT
- Token válido por exatamente **8 horas** sem margem de tolerância (`ClockSkew = TimeSpan.Zero`).

---

## 9. Testes

### Domain Tests (`xUnit + FluentAssertions`)
- `CoordinatesTests` — 8 testes: criação, Haversine, raio, igualdade
- `VisitTests` — 9 testes: check-in, check-out, todas as regras de negócio
- `CpfTests` / `CnpjTests` — validação dos algoritmos

### Application Tests (`xUnit + Moq + FluentAssertions`)
- `RegisterCheckinCommandHandlerTests` — 5 testes: sucesso, fora de raio, conflito, não encontrado

### Integration Tests (`WebApplicationFactory + InMemory EF Core`)
- `AuthControllerIntegrationTests` — testes de ponta a ponta sem banco real

```bash
# Executar todos os testes
dotnet test

# Com relatório de cobertura
dotnet test --collect:"XPlat Code Coverage"
```

---

## 10. Executando o Projeto

### Pré-requisitos
- .NET 9 SDK
- SQL Server (local ou Docker)
- (Opcional) Docker

### Com Docker

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=SuaSenha123!" \
  -p 1433:1433 --name sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

### Configuração

Edite `src/FSI.SupportPointSystem.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=FSI_SupportPoint;User Id=sa;Password=SuaSenha123!;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "SecretKey": "SUA_CHAVE_SECRETA_MINIMO_32_CHARS!",
    "Issuer": "FSI.SupportPointSystem.Api",
    "Audience": "FSI.SupportPointSystem.Clients"
  }
}
```

### Executando

```bash
# Aplicar migrations e iniciar API
cd src/FSI.SupportPointSystem.Api
dotnet run
```

Swagger disponível em: `https://localhost:5001/swagger`

---

## 11. Migrations e Banco de Dados

```bash
# Adicionar nova migration
dotnet ef migrations add NomeDaMigration \
  --project src/FSI.SupportPointSystem.Infrastructure \
  --startup-project src/FSI.SupportPointSystem.Api

# Aplicar ao banco
dotnet ef database update \
  --project src/FSI.SupportPointSystem.Infrastructure \
  --startup-project src/FSI.SupportPointSystem.Api
```

### Índices criados automaticamente

| Tabela | Índice | Tipo |
|---|---|---|
| `Users` | `Cpf` | UNIQUE |
| `Customers` | `Cnpj` | UNIQUE |
| `Visits` | `SellerId WHERE CheckoutTimestamp IS NULL` | FILTERED (visita ativa) |

---

## 12. Endpoints da API

### Auth
```
POST /api/auth/login
Body: { "cpf": "529.982.247-25", "password": "senha123!" }
```

### Sellers (ADMIN)
```
POST /api/sellers
Body: { "cpf": "...", "password": "...", "name": "...", "phone": "...", "email": "..." }
```

### Customers (ADMIN)
```
POST /api/customers
Body: { "companyName": "...", "cnpj": "...", "latitude": -23.55, "longitude": -46.63 }
```

### Visits (SELLER)
```
POST /api/visits/checkin
Body: { "customerId": "uuid", "latitude": -23.550600, "longitude": -46.633400 }

POST /api/visits/checkout
Body: { "latitude": -23.550600, "longitude": -46.633400, "summary": "Texto opcional" }

GET /api/visits/history?page=1&pageSize=20
```

---

## 13. Diferenças em Relação à v1

| Componente | v1 | v2 |
|---|---|---|
| **Runtime** | .NET (não especificado) | **.NET 9 / C# 13** |
| **Padrão de comunicação** | AppService direto | **MediatR CQRS** |
| **Resposta de erro** | Exceção de controle de fluxo | **Result Pattern** |
| **Lógica de distância** | `LocationService` separado | **Encapsulado em `Coordinates`** |
| **Raio de 100m** | `if (distance > 100) throw` no construtor da entidade | **`Visit.RegisterCheckin()` delega ao `Customer.IsWithinCheckinRadius()`** |
| **Validação** | `IValidator` chamado no AppService | **Pipeline Behavior automático** |
| **Domain Events** | Ausentes | **Presentes e dispatchados pós-commit** |
| **Unit of Work** | Ausente | **`IUnitOfWork` explícito** |
| **Testes** | Ausentes | **Domain + Application + Integration** |
| **BDD** | Feature existia mas era decorativo | **Feature detalhada com 20+ cenários** |
| **Segurança JWT** | `ClockSkew` padrão (5min) | **`ClockSkew = Zero`** |
| **Hash de senha** | Não especificado | **BCrypt work factor 12** |
