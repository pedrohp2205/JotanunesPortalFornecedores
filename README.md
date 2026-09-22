# Jotanunes - Portal de Fornecedores

Sistema de Gestão e Validação Documental de Fornecedores da Jotanunes.

Projeto em **.NET 8**, seguindo **Clean Architecture**, **DDD**, **Unit of Work**, **IoC** e **Docker**, na mesma organização de camadas do projeto de referência `TClient-API`. Banco de dados **SQL Server**.

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
├── Jotanunes.Infra.Storage    # Armazenamento de documentos (S3)
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
| PUT | `/api/company/{id}/supplier-type` | Altera o tipo de fornecimento (1 material, 2 mão de obra, 3 os dois). Recusa remover um tipo que ainda tenha solicitação ativa |
| DELETE | `/api/company/{id}` | Exclusão lógica |
| GET | `/api/company/{id}/users` | Lista os acessos da empresa |
| POST | `/api/company/{id}/users` | Cria o acesso ao portal (pré-requisito do login) |
| POST | `/api/company/{id}/users/{userId}/deactivate` | Desativa o acesso e revoga a sessão ativa |
| POST | `/api/company/{id}/users/{userId}/activate` | Reativa o acesso |
| POST | `/api/company/{id}/users/{userId}/reset-password` | Define uma senha provisória (`temporaryPassword`), destrava o usuário, revoga a sessão, exige troca no próximo login e avisa o usuário por e-mail |

### Frente externa - autenticação do fornecedor

| Método | Rota | Auth | Descrição |
| --- | --- | --- | --- |
| POST | `/api/auth/login` | Não | Autentica e devolve access token + refresh token |
| POST | `/api/auth/refresh` | Não | Renova o par de tokens |
| POST | `/api/auth/logout` | Sim | Revoga o refresh token |
| GET | `/api/auth/me` | Sim | Dados do usuário autenticado |
| POST | `/api/auth/change-password` | Sim | Troca de senha |
| POST | `/api/auth/forgot-password` | Não | Envia por e-mail um link de redefinição. Responde sempre 204, exista o e-mail ou não |
| POST | `/api/auth/reset-password` | Não | Define a nova senha com `email` + `token` recebido por e-mail |

Regras aplicadas na autenticação:

- senha com hash **BCrypt** (work factor 12);
- mensagem única para e-mail inexistente e senha errada, evitando enumeração de usuários;
- bloqueio temporário de 15 minutos após 5 tentativas inválidas;
- token carrega a claim `company_id`, base para o isolamento por empresa exigido pelo RNF01;
- troca de senha revoga o refresh token ativo.
- "esqueci minha senha": o token é aleatório, vale 60 minutos, é de uso único e só o hash (SHA-256) fica no banco. Novo pedido só é aceito após 2 minutos, para não inundar a caixa de e-mail. O link aponta para `Email:PortalUrl` + `Email:ResetPasswordPath` (padrão `/redefinir-senha`) com `?email=...&token=...`; sem `PortalUrl`, o token segue em texto no e-mail.

---

### Solicitação (intermedia a Jotanunes e o fornecedor)

A **solicitação** (`SupplyRequest`) é o pedido da Jotanunes a uma empresa para uma obra. Ela define o tipo de fornecimento (`Material` = 1 ou `ManpowerLabor` = 2) e é a âncora dos documentos recorrentes. Uma empresa pode fornecer os dois tipos (`supplierType: 3` no cadastro da empresa); nesse caso a Jotanunes abre **uma solicitação por tipo**, cada uma com seu checklist. A habilitação da empresa usa a união dos tipos dela.

Status: `Open` (1) → `InProgress` (2, no primeiro documento enviado) → `Completed` (3) ou `Cancelled` (4). Solicitação encerrada não recebe documentos e nunca tem pendência. `Complete` só é aceito quando a solicitação não tem pendências no período corrente (o mesmo critério do campo `pending`, abaixo); com pendências, a resposta é 400 explicando o que falta, e o caminho é `Cancel`. Só pode existir uma solicitação ativa por empresa, obra e tipo.

| Frente | Método | Rota | Descrição |
| --- | --- | --- | --- |
| Interna | GET | `/api/supplyrequest` | Lista, com filtros `companyId`, `workSiteId`, `supplierType`, `status` e `hasPending` |
| Interna | GET | `/api/supplyrequest/{id}` | Consulta uma solicitação |
| Interna | POST | `/api/supplyrequest` | Abre a solicitação (`companyId`, `workSiteId`, `supplierType`, `requiredWorkerCount`) |
| Interna | PUT | `/api/supplyrequest/{id}` | Ajusta a quantidade de trabalhadores (só mão de obra) |
| Interna | POST | `/api/supplyrequest/{id}/complete` | Conclui |
| Interna | POST | `/api/supplyrequest/{id}/cancel` | Cancela |
| Externa | GET | `/api/supplyrequest` | Solicitações da empresa do token |
| Externa | GET | `/api/supplyrequest/{id}` | Consulta uma solicitação da empresa |

**Pendências (`pending`).** Toda solicitação devolvida pelas duas frentes traz o campo `pending`, calculado para o período corrente da obra: `periodStart`/`periodEnd`, `missingOnboardingCount`, `missingRecurringCompanyCount`, `requiredWorkerCount` e `workersUpToDate`. Vem `null` quando a solicitação está em dia ou encerrada. Só documento **aprovado** conta como entregue. O filtro `hasPending=true|false` em `GET /api/supplyrequest` lista só as solicitações com (ou sem) pendência, e substitui o antigo `GET /api/document/overdue`. O detalhe do que falta continua em `GET /api/document/checklist/{supplyRequestId}`. O cálculo é feito em lote (3 consultas para a lista inteira, independentemente do tamanho).

**Checklist por item.** Cada item de `GET /api/document/checklist/{supplyRequestId}` traz `status` (`NotSent` 0, `Pending` 1, `Rejected` 2, `Approved` 3), `statusDescription`, `documentId` do envio que define o estado (o aprovado; senão o último em análise; senão o último recusado) e `rejectionReason` quando recusado. Aprovado prevalece sobre em análise, que prevalece sobre recusado: um reenvio já tira o item de "recusado". `isSatisfied` continua significando "há documento aprovado". Tipos condicionais (`isConditional`) entram como itens **opcionais** (`isRequired: false`, com `conditionDescription`): podem ser enviados e acompanhados, mas não contam como pendência nem na habilitação.

**Catálogo para o fornecedor.** `GET /api/documenttype` (frente externa) lista os tipos ativos aplicáveis à empresa do token, com `category`, `subject` (`Company` ou `Worker`), `requiresExpirationDate`, `isConditional` e `conditionDescription`. O parâmetro opcional `supplierType` (1 material, 2 mão de obra) restringe a um dos tipos que a empresa fornece. É com ele que o front monta o formulário de "informar trabalhador" (tipos com `subject = Worker`) mesmo antes de existir qualquer trabalhador no checklist.

O upload de documento (`POST /api/document`) recebe `supplyRequestId` nos documentos recorrentes; o checklist fica em `GET /api/document/checklist/{supplyRequestId}` nas duas frentes.

Regras de conferência no envio: o tipo de documento precisa se aplicar ao tipo de fornecimento da solicitação (ou aos tipos da empresa, na habilitação). Documentos condicionais não entram na conta da habilitação e a data de validade é apenas armazenada, não avaliada.

---

## Pré-requisitos

- .NET SDK 8
- Docker e Docker Compose (para subir o ambiente completo)
- SQL Server (se for rodar fora do Docker)

---

## Executando com Docker Compose

```bash
docker-compose up -d --build
```

Sobe o SQL Server, a API interna na porta `8080` e a API externa na porta `80`. As migrations são aplicadas automaticamente pela API interna, que é a dona do schema.

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

## Armazenamento de documentos (S3)

A porta `IDocumentStorageService` (`Jotanunes.Application`) abstrai o upload, download, exclusão e checagem de existência de um documento por chave. A implementação (`Jotanunes.Infra.Storage`) usa o AWS SDK (`AWSSDK.S3`) e é registrada em `Jotanunes.Infra.IoC`, disponível para as duas frentes.

O provedor usado é o **Cloudflare R2** (compatível com a API do S3), mas a implementação funciona com qualquer endpoint S3-compatível, inclusive AWS S3 real.

Configuração (seção `S3` do `appsettings.json`, ou variáveis `S3__*`):

| Campo | Descrição |
| --- | --- |
| `BucketName` | Bucket onde os documentos são salvos (obrigatório) |
| `Region` | Região. No R2, sempre `auto` (obrigatório) |
| `AccessKey` / `SecretKey` | Credenciais do token de API R2 (ou de um IAM key na AWS). Em produção na AWS real, podem ficar em branco para usar a cadeia padrão de credenciais (IAM role, variáveis `AWS_*`, etc.) |
| `ServiceUrl` | Endpoint da conta no R2: `https://<ACCOUNT_ID>.r2.cloudflarestorage.com`. Na AWS real, deixe vazio |
| `ForcePathStyle` | Mantenha `true` para R2 e para a maioria dos endpoints S3-compatíveis |

O `appsettings.json` traz apenas um `ServiceUrl` de exemplo (`https://<ACCOUNT_ID>.r2.cloudflarestorage.com`) e credenciais em branco. Para rodar localmente, preencha `AccessKey`/`SecretKey`/`ServiceUrl` com os dados do seu bucket R2 via `appsettings.Development.json` (não versionado) ou `dotnet user-secrets`. Para o `docker-compose`, crie um arquivo `.env` (já ignorado pelo git) na raiz do projeto com:

```
R2_BUCKET_NAME=jotanunes-documentos
R2_ACCESS_KEY_ID=...
R2_SECRET_ACCESS_KEY=...
R2_ENDPOINT=https://<ACCOUNT_ID>.r2.cloudflarestorage.com
```

---

## Configuração sensível

O `Jwt:SecretKey` do `appsettings.json` é um valor de desenvolvimento. Em produção ele deve vir de variável de ambiente (`Jwt__SecretKey`) ou cofre de segredos, com no mínimo 32 caracteres — a aplicação recusa subir se isso não for atendido.

As credenciais do `S3` (`AccessKey`/`SecretKey`/`ServiceUrl`) não devem ser versionadas — preencha localmente via `appsettings.Development.json`/`dotnet user-secrets`, e em produção via variáveis de ambiente ou cofre de segredos.
