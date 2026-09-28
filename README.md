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
├── Jotanunes.Infra.DocumentAi # Leitura de documentos (PdfPig) e worker da análise automática
├── Jotanunes.Infra.IoC        # Injeção de dependências
├── Jotanunes.AppHost          # Orquestração local com .NET Aspire (SQL Server + APIs)
├── Jotanunes.Tests            # Testes unitários (xUnit)
├── Jotanunes.External.BddTests # Testes de integração/BDD da API externa (Reqnroll)
├── Jotanunes.Internal.BddTests # Testes de integração/BDD da API interna (Reqnroll)
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
| POST | `/api/auth/refresh` | Não | Renova o par de tokens (o refresh token é rotacionado a cada uso) |
| POST | `/api/auth/logout` | Sim | Encerra a sessão atual (as de outros dispositivos continuam) |
| GET | `/api/auth/me` | Sim | Dados do usuário autenticado |
| POST | `/api/auth/change-password` | Sim | Troca de senha. Encerra todas as sessões e devolve um par de tokens novo |
| POST | `/api/auth/forgot-password` | Não | Envia por e-mail um link de redefinição. Responde sempre 204, exista o e-mail ou não |
| POST | `/api/auth/reset-password` | Não | Define a nova senha com `email` + `token` recebido por e-mail |

Regras aplicadas na autenticação:

- senha com hash **BCrypt** (work factor 12);
- mensagem única para e-mail inexistente e senha errada, evitando enumeração de usuários;
- bloqueio temporário de 15 minutos após 5 tentativas inválidas;
- token carrega a claim `company_id`, base para o isolamento por empresa exigido pelo RNF01, e `sid`, a sessão que o emitiu;
- **senha provisória**: enquanto `mustChangePassword` for verdadeiro, só `me`, `change-password` e `logout` respondem; os demais endpoints devolvem 403 com `{"Message": "...", "MustChangePassword": true}`. Como a troca derruba a sessão atual, `change-password` devolve tokens novos (já sem a restrição);
- **sessões**: cada login abre uma sessão (`supplier_user_sessions`), então o mesmo usuário pode estar em vários dispositivos. Só o hash SHA-256 do refresh token fica no banco. Cada `refresh` rotaciona o token; reapresentar o token anterior depois de 30 segundos da rotação é tratado como vazamento e encerra a sessão (dentro dos 30 segundos, é tratado como duas abas renovando juntas e só recusado);
- troca de senha, redefinição e desativação do usuário encerram todas as sessões (via `SecurityStamp` do usuário).
- "esqueci minha senha": o token é aleatório, vale 60 minutos, é de uso único e só o hash (SHA-256) fica no banco. Novo pedido só é aceito após 2 minutos, para não inundar a caixa de e-mail. O link aponta para `Email:PortalUrl` + `Email:ResetPasswordPath` (padrão `/redefinir-senha`) com `?email=...&token=...`; sem `PortalUrl`, o token segue em texto no e-mail.

---

### Solicitação (intermedia a Jotanunes e o fornecedor)

A **solicitação** (`SupplyRequest`) é o pedido da Jotanunes a uma empresa para uma obra. Ela define o tipo de fornecimento (`Material` = 1 ou `ManpowerLabor` = 2) e é a âncora dos documentos recorrentes. Uma empresa pode fornecer os dois tipos (`supplierType: 3` no cadastro da empresa); nesse caso a Jotanunes abre **uma solicitação por tipo**, cada uma com seu checklist. A habilitação da empresa usa a união dos tipos dela.

Status: `Open` (1) → `InProgress` (2, no primeiro documento enviado) → `Completed` (3) ou `Cancelled` (4). Solicitação encerrada não recebe documentos e nunca tem pendência. `Complete` só é aceito quando a solicitação não tem pendências no período corrente (o mesmo critério do campo `pending`, abaixo); com pendências, a resposta é 400 explicando o que falta, e o caminho é `Cancel`. Só pode existir uma solicitação ativa por empresa, obra e tipo.

| Frente | Método | Rota | Descrição |
| --- | --- | --- | --- |
| Interna | GET | `/api/supplyrequest` | Lista paginada (`pageNumber`, `pageSize`), com filtros `companyId`, `workSiteId`, `supplierType`, `status` e `hasPending` |
| Interna | GET | `/api/supplyrequest/{id}` | Consulta uma solicitação |
| Interna | POST | `/api/supplyrequest` | Abre a solicitação (`companyId`, `workSiteId`, `supplierType`, `requiredWorkerCount`) |
| Interna | PUT | `/api/supplyrequest/{id}` | Ajusta a quantidade de trabalhadores (só mão de obra) |
| Interna | POST | `/api/supplyrequest/{id}/complete` | Conclui |
| Interna | POST | `/api/supplyrequest/{id}/cancel` | Cancela |
| Externa | GET | `/api/supplyrequest` | Solicitações da empresa do token (paginada) |
| Externa | GET | `/api/supplyrequest/{id}` | Consulta uma solicitação da empresa |

**Pendências (`pending`).** Toda solicitação devolvida pelas duas frentes traz o campo `pending`, calculado para o período corrente da obra: `periodStart`/`periodEnd`, `missingOnboardingCount`, `missingRecurringCompanyCount`, `requiredWorkerCount` e `workersUpToDate`. Vem `null` quando a solicitação está em dia ou encerrada. Só documento **aprovado** conta como entregue. O filtro `hasPending=true|false` em `GET /api/supplyrequest` lista só as solicitações com (ou sem) pendência, e substitui o antigo `GET /api/document/overdue`. O detalhe do que falta continua em `GET /api/document/checklist/{supplyRequestId}`. O cálculo é feito em lote: uma consulta para os documentos recorrentes (só os que alcançam o período corrente), uma para a habilitação de todas as empresas e uma por tipo de fornecimento para o catálogo, independentemente do tamanho da lista. Sem `hasPending`, a paginação é feita no banco e só a página é calculada. Com `hasPending`, a pendência precisa ser calculada antes de filtrar: `hasPending=true` carrega só as solicitações ativas (encerradas nunca têm pendência) e pagina em memória.

**Checklist por item.** Cada item de `GET /api/document/checklist/{supplyRequestId}` traz `status` (`NotSent` 0, `Pending` 1, `Rejected` 2, `Approved` 3), `statusDescription`, `documentId` do envio que define o estado (o aprovado; senão o último em análise; senão o último recusado) e `rejectionReason` quando recusado. Aprovado prevalece sobre em análise, que prevalece sobre recusado: um reenvio já tira o item de "recusado". `isSatisfied` continua significando "há documento aprovado". Tipos condicionais (`isConditional`) entram como itens **opcionais** (`isRequired: false`, com `conditionDescription`): podem ser enviados e acompanhados, mas não contam como pendência nem na habilitação.

**Catálogo para o fornecedor.** `GET /api/documenttype` (frente externa) lista os tipos ativos aplicáveis à empresa do token, com `category`, `subject` (`Company` ou `Worker`), `requiresExpirationDate`, `isConditional` e `conditionDescription`. O parâmetro opcional `supplierType` (1 material, 2 mão de obra) restringe a um dos tipos que a empresa fornece. É com ele que o front monta o formulário de "informar trabalhador" (tipos com `subject = Worker`) mesmo antes de existir qualquer trabalhador no checklist.

O upload de documento (`POST /api/document`) recebe `supplyRequestId` nos documentos recorrentes; o checklist fica em `GET /api/document/checklist/{supplyRequestId}` nas duas frentes.

Regras do arquivo no upload: até 20 MB, não vazio, e só PDF, PNG ou JPEG. O formato é conferido pela assinatura dos primeiros bytes, não pela extensão nem pelo `Content-Type` enviado; o tipo gravado (e devolvido no download) é o detectado. Se o registro não puder ser salvo no banco depois do envio ao bucket, o arquivo é removido do bucket.

Regras de conferência no envio: o tipo de documento precisa se aplicar ao tipo de fornecimento da solicitação (ou aos tipos da empresa, na habilitação). Documentos condicionais não entram na conta da habilitação. Quando o tipo tem `requiresExpirationDate`, a data de validade é obrigatória, e um documento já vencido é recusado no envio.

### Trabalhadores e alocação

O fornecedor mantém o cadastro dos próprios trabalhadores (`Worker`: nome, CPF e ativo). O CPF é único por empresa e não muda depois do cadastro. A presença numa obra é registrada pela **alocação** (`WorkerAllocation`) do trabalhador numa solicitação de mão de obra. Ao desalocar, o registro é mantido com `releasedAt`, como histórico.

| Frente | Método | Rota | Descrição |
| --- | --- | --- | --- |
| Externa | GET | `/api/worker` | Lista paginada dos trabalhadores da empresa (filtros `name`, `cpf`, `active`, `supplyRequestId`) |
| Externa | GET | `/api/worker/{id}` | Consulta um trabalhador |
| Externa | POST | `/api/worker` | Cadastra (`name`, `cpf`) |
| Externa | PUT | `/api/worker/{id}` | Altera o nome |
| Externa | POST | `/api/worker/{id}/deactivate` | Desativa e desaloca o trabalhador das solicitações ativas |
| Externa | POST | `/api/worker/{id}/activate` | Reativa |
| Externa | GET | `/api/supplyrequest/{id}/workers` | Trabalhadores alocados (`includeReleased=true` inclui o histórico) |
| Externa | POST | `/api/supplyrequest/{id}/workers` | Aloca (`workerId`) |
| Externa | DELETE | `/api/supplyrequest/{id}/workers/{workerId}` | Desaloca |
| Interna | GET | `/api/worker`, `/api/worker/{id}` | Consulta trabalhadores (filtro `companyId`) |
| Interna | GET | `/api/supplyrequest/{id}/workers` | Trabalhadores alocados |
| Interna | DELETE | `/api/supplyrequest/{id}/workers/{workerId}` | Desaloca |

Regras de alocação:
- Só vale para solicitações de mão de obra abertas, com trabalhador ativo da mesma empresa.
- O `requiredWorkerCount` limita quantos podem estar alocados ao mesmo tempo. Para trocar alguém, desaloca-se antes.

Documentos de trabalhador recebem `workerId` (não mais nome e CPF livres):
- **Recorrentes** (`category = Recurring`, `subject = Worker`) exigem que o trabalhador esteja alocado na solicitação.
- **Habilitação do trabalhador** (`category = Onboarding`, `subject = Worker`, ex.: ASO, NR-35) é enviada sem `supplyRequestId`. Fica ligada ao trabalhador e vale em qualquer obra em que ele for alocado, até a data de validade. Não precisa ser reenviada ao trocar de obra.

No checklist, `workers` lista os **alocados**, com ou sem documento enviado. Cada um traz:
- `onboardingItems`: a habilitação do trabalhador. Um aprovado vencido aparece como `Expired` (4) e não satisfaz o item.
- `items`: os recorrentes do período.

O checklist também passa a trazer `allocatedWorkerCount`. Tipos de habilitação de trabalhador não entram na habilitação da empresa.

### Análise automática de documentos

Todo upload grava, na mesma transação, uma análise `Pending` em `document_analyses`. Um worker (`DocumentAnalysisWorker`) busca as pendentes no banco, lê o arquivo e produz um **parecer sugerido**: campos extraídos, achados e um veredito. **Não aprova nem recusa nada**: `approve`/`reject` continuam manuais, e o parecer serve de apoio ao analista.

**Cadeia de fallback.** Cada tipo de documento tem um analisador (`IDocumentTypeAnalyzer`, escolhido pelo `code` do tipo) que declara os campos obrigatórios. A leitura tenta os extratores (`IDocumentTextExtractor`) do mais barato ao mais caro (`NativeText` → `Ocr` → `Vision`) e para no primeiro que lê todos os obrigatórios. CNPJ ou CPF com dígito verificador inválido conta como não lido, o que faz a leitura subir de nível. Se nenhum nível completa, a análise vai para `ManualReviewRequired` com a lista do que faltou. Por enquanto só existe o nível `NativeText` (camada de texto do PDF, via PdfPig); OCR e modelo de visão entram nas próximas etapas.

| Status | Significado |
|---|---|
| `Pending` (1) | Na fila do worker |
| `Completed` (2) | Lido; ver `verdict` e `findings` |
| `ManualReviewRequired` (3) | Nenhum nível leu todos os campos obrigatórios; `failureReason` diz quais faltaram |
| `Failed` (4) | Erro técnico (ex.: bucket indisponível) em `MaxAttempts` (3) tentativas seguidas |
| `NotSupported` (5) | Ainda não há analisador para o código do tipo |

Veredito: `Conforming` (1) sem achados relevantes, `NeedsAttention` (2) com algum `Warning` e `NonConforming` (3) com algum `Blocking`. Achados `Info` só informam (ex.: validade identificada na certidão).

Analisadores disponíveis:

| Código do tipo | Documento | Confere |
|---|---|---|
| `FGTS_CND` (id 4, "Certidão Negativa de FGTS") | Certificado de Regularidade do FGTS (CRF) | CNPJ igual ao da empresa; se está vencida, se ainda não vale ou se vence em até 7 dias; validade informada no envio × certidão; razão social |

Endpoints (frente interna):

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/document/{id}/analysis` | Parecer do documento |
| POST | `/api/document/{id}/analysis` | Recoloca a análise na fila (202). Também cria a análise de documentos enviados antes desta funcionalidade |

Configuração (`DocumentAnalysis`): `WorkerEnabled` (ligado só na frente externa, porque o worker não trava as linhas e **deve rodar em um único processo**), `PollingIntervalSeconds` (10) e `BatchSize` (10).

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

## Executando com Aspire

O `Jotanunes.AppHost` (.NET Aspire 13, exige o SDK do .NET 10) sobe o SQL Server em container, cria as bases `jotanunes_portal` (dev), `jotanunes_portal_test_external` e `jotanunes_portal_test_internal` (uma para cada projeto `*.BddTests`), aplica as migrations do EF Core e inicia as duas APIs já apontando para a base de dev. O dashboard mostra o status, os logs e o health de cada recurso.

```bash
dotnet run --project Jotanunes.AppHost    # ou: aspire run
```

- **SQL Server**: porta fixa `1433`, senha `SenhaForte123!` (parâmetro `sql-password` em `Jotanunes.AppHost/appsettings.json`; sobrescreva em user secrets se quiser). O container é persistente e os dados ficam em volume, então sobrevivem ao encerramento do AppHost. Pare o `sqlserver` do Docker Compose antes, porque os dois usam a porta 1433.
- **Migrations**: aplicadas automaticamente quando o banco fica pronto. O comando **Apply EF Migrations**, no menu de cada base no dashboard, reaplica sob demanda.
- **Docker ou Podman**: o Aspire funciona com os dois. Por padrão ele detecta o runtime instalado. Para forçar o Podman quando os dois estiverem instalados, exporte `ASPIRE_CONTAINER_RUNTIME=podman` antes de rodar.

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

São três projetos:

| Projeto | Tipo | Depende de banco |
| --- | --- | --- |
| `Jotanunes.Tests` | Unitários (xUnit + Moq), organizados em pastas que espelham as camadas: `Domain/`, `Application/`, `Infra/` | Não |
| `Jotanunes.External.BddTests` | Integração/BDD (Reqnroll + xUnit v3) da API externa, via `WebApplicationFactory` | Sim (SQL Server) |
| `Jotanunes.Internal.BddTests` | Integração/BDD (Reqnroll + xUnit v3) da API interna, via `WebApplicationFactory` | Sim (SQL Server) |

```bash
docker compose up -d sqlserver   # necessário só para os projetos *.BddTests
dotnet test
```

### Projetos BDD

Cada frente tem seu próprio projeto, autocontido (sem biblioteca compartilhada entre eles). Os cenários ficam em `Features/` (Gherkin em pt-BR), com `Api/` para os endpoints e `TestingEnvironmentSetup/` para validar o próprio ambiente de teste. A organização:

```
Jotanunes.{External,Internal}.BddTests/
├── Features/            # .feature (Funcionalidade / Cenário / Esquema do Cenário)
├── StepDefinitions/     # Bindings, espelhando Features/; passos reutilizáveis em Shared/
├── Drivers/             # Construção de entidades e DTOs válidos para os cenários
└── Support/
    ├── Contexts/        # Estado compartilhado entre passos de um cenário (Given/HttpResponse)
    ├── Fixtures/        # Configuração, banco e WebApplicationFactory da API
    │   └── ApiClients/  # Um cliente HTTP por controller
    └── Models/          # Modelos de resposta usados só nos testes
```

- **Banco**: cada projeto usa a sua base (`jotanunes_portal_test_external` e `jotanunes_portal_test_internal`), criada e migrada automaticamente. São bases separadas porque o `dotnet test` roda os dois projetos em paralelo e, numa base só, os cenários de um disputariam os mesmos registros com os do outro. Cada cenário roda dentro de uma transação que é desfeita ao final, então os testes não deixam dados. Para apontar para outro servidor, defina `DataBase:ConnectionString` em user secrets (`dotnet user-secrets set "DataBase:ConnectionString" "..." --project Jotanunes.External.BddTests`, e o mesmo para o `Internal`) ou pela variável `DataBase__ConnectionString`. A variável `DATABASE` usada pelas APIs tem precedência sobre essa configuração, então não a deixe exportada no shell ao rodar os testes.
- **Autenticação** (externa): os passos `Dado que eu estou autenticado ...` fazem login real em `/api/auth/login` e usam o JWT devolvido. A frente interna ainda não tem autenticação.
- **Armazenamento**: `IDocumentStorageService` é substituído por um mock; e-mail usa o `LogEmailSender`.

**Golden set da análise automática.** `GoldenSetTests` roda os analisadores contra documentos reais anotados em `Documentos Jotanunes/golden-set.json`. A pasta fica fora do git porque os arquivos têm dados pessoais; sem ela, o teste passa sem conferir nada. Cada caso informa o arquivo, o código do tipo, se a leitura deve ser completa (`expectComplete`) e os campos esperados (`expected`). Casos com `expectComplete: false` garantem que um documento errado (ex.: CNH enviada no lugar da CRF) não seja aceito como aquele tipo. Ao escrever um analisador novo, anote primeiro os arquivos reais dele aqui.

---

## Convenções adotadas

- Identificadores, arquivos e pastas em **inglês**; mensagens de erro retornadas pela API em **português**.
- Entidades com setters privados: escrita passa por construtor e métodos de domínio, e o AutoMapper é usado apenas no sentido entidade → DTO, para que as validações não sejam contornadas.
- Exclusão lógica via `DeletedAt` com query filter global em `BaseEntity`.
- Erros tratados em um único middleware: `JotanunesException` → 400, `UnauthorizedAccessException` → 401, `KeyNotFoundException` → 404.
- Unicidade garantida também no banco (CNPJ, e-mail de usuário, código de tipo de documento, solicitação ativa por empresa/obra/tipo). Quando duas requisições simultâneas passam pela checagem do serviço, o `UnitOfWork` converte a violação de índice único em `JotanunesException` (400) com a mesma mensagem.

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
