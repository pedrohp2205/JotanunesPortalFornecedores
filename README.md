# Jotanunes - Portal de Fornecedores

Sistema de Gestão e Validação Documental de Fornecedores da Jotanunes.

Projeto em **.NET 10**, seguindo **Clean Architecture**, **DDD**, **Unit of Work**, **IoC** e **Docker**, na mesma organização de camadas do projeto de referência `TClient-API`.

---

## Arquitetura: duas frentes

O sistema é dividido em duas frentes com hosts independentes que compartilham as mesmas camadas de domínio, aplicação e dados:

| Frente | Projeto | Público | Autenticação | Porta (docker) |
| --- | --- | --- | --- | --- |
| Externa | `Jotanunes.API.External` | Fornecedores (Portal do Fornecedor) | JWT | 80 |
| Interna | `Jotanunes.API.Internal` | Equipe da Jotanunes (back-office) | Sem auth nesta etapa | 8080 |

---

## Estrutura do projeto

```
JotanunesPortalFornecedores/
│
├── Jotanunes.API.External     # Host da frente externa (auth de fornecedor)
├── Jotanunes.API.Internal     # Host da frente interna (CRUD de empresas)
├── Jotanunes.API.Shared       # Middleware de exceções e extensões de claims
├── Jotanunes.Application      # DTOs, interfaces e serviços de aplicação
├── Jotanunes.Domain           # Entidades, regras de negócio, contratos de repositório
├── Jotanunes.Infra.Data       # DbContext, configurations, repositórios, migrations
├── Jotanunes.Infra.Security   # Hash de senha (BCrypt) e emissão de JWT
├── Jotanunes.Infra.IoC        # Injeção de dependências
├── Jotanunes.Tests            # Testes unitários de domínio (xUnit)
├── Dockerfile.external
├── Dockerfile.internal
└── docker-compose.yml
```

O fluxo de dependência aponta sempre para dentro: as APIs conhecem a IoC, a IoC conhece Application/Infra, e o Domain não conhece ninguém.

---

## Pré-requisitos

- .NET SDK 10
- Docker e Docker Compose (para subir o ambiente completo)
- PostgreSQL (se for rodar fora do Docker)

---

## Executando com Docker Compose

```bash
docker-compose up -d --build
```

Sobe o PostgreSQL, a API interna na porta `8080` e a API externa na porta `80`. As migrations são aplicadas automaticamente pela API interna, que é a dona do schema.

Swagger:

- Interna: http://localhost:8080/swagger
- Externa: http://localhost/swagger

---

## Executando localmente

```bash
dotnet run --project Jotanunes.API.Internal   # http://localhost:5200/swagger
dotnet run --project Jotanunes.API.External   # http://localhost:5100/swagger
```

A connection string vem da variável de ambiente `DATABASE` ou, na falta dela, de `ConnectionStrings:ConnectionString` no `appsettings.json`.

---

## Migrations

```bash
dotnet ef migrations add NomeDaMigration --project Jotanunes.Infra.Data --startup-project Jotanunes.API.Internal
dotnet ef database update --project Jotanunes.Infra.Data --startup-project Jotanunes.API.Internal
```

---

## Testes

```bash
dotnet test
```

---

## Convenções adotadas

- Identificadores, arquivos e pastas em **inglês**; mensagens de erro retornadas pela API em **português**.
- Entidades com setters privados: escrita passa por construtor e métodos de domínio, e o AutoMapper é usado apenas no sentido entidade → DTO, para que as validações não sejam contornadas.
- Exclusão lógica via `DeletedAt` com query filter global em `BaseEntity`.
- Erros tratados em um único middleware: `JotanunesException` → 400, `KeyNotFoundException` → 404.
