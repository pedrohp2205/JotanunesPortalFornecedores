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

## Escopo implementado

### Frente interna - CRUD de empresas

| Método | Rota | Descrição |
| --- | --- | --- |
| GET | `/api/company` | Lista paginada com filtro por razão social, nome fantasia e CNPJ |
| GET | `/api/company/{id}` | Consulta uma empresa |
| POST | `/api/company` | Cadastra empresa |
| PUT | `/api/company/{id}` | Atualiza empresa (o CNPJ não é editável) |
| DELETE | `/api/company/{id}` | Exclusão lógica |
| GET | `/api/company/{id}/users` | Lista os acessos da empresa |
| POST | `/api/company/{id}/users` | Cria o acesso ao portal (pré-requisito do login) |

### Frente externa - autenticação do fornecedor

| Método | Rota | Auth | Descrição |
| --- | --- | --- | --- |
| POST | `/api/auth/login` | Não | Autentica e devolve access token + refresh token |
| POST | `/api/auth/refresh` | Não | Renova o par de tokens |
| POST | `/api/auth/logout` | Sim | Revoga o refresh token |
| GET | `/api/auth/me` | Sim | Dados do usuário autenticado |
| POST | `/api/auth/change-password` | Sim | Troca de senha |

Regras aplicadas na autenticação:

- senha com hash **BCrypt** (work factor 12);
- mensagem única para e-mail inexistente e senha errada, evitando enumeração de usuários;
- bloqueio temporário de 15 minutos após 5 tentativas inválidas;
- token carrega a claim `company_id`, base para o isolamento por empresa exigido pelo RNF01;
- troca de senha revoga o refresh token ativo.

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
- Erros tratados em um único middleware: `JotanunesException` → 400, `UnauthorizedAccessException` → 401, `KeyNotFoundException` → 404.

---

## Configuração sensível

O `Jwt:SecretKey` do `appsettings.json` é um valor de desenvolvimento. Em produção ele deve vir de variável de ambiente (`Jwt__SecretKey`) ou cofre de segredos, com no mínimo 32 caracteres — a aplicação recusa subir se isso não for atendido.
