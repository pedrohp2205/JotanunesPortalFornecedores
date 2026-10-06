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
├── Jotanunes.Infra.DocumentAi # Leitura de documentos (PdfPig)
├── Jotanunes.Infra.IoC        # Injeção de dependências
├── Jotanunes.Worker           # Processo de jobs agendados por cron (análise automática de documentos)
├── Jotanunes.AppHost          # Orquestração local com .NET Aspire (SQL Server + APIs + worker)
├── Jotanunes.Tests            # Testes unitários (xUnit)
├── Jotanunes.Worker.Tests     # Testes do worker: jobs e agendamentos (xUnit v3)
├── Jotanunes.External.BddTests # Testes de integração/BDD da API externa (Reqnroll)
├── Jotanunes.Internal.BddTests # Testes de integração/BDD da API interna (Reqnroll)
├── Dockerfile.external
├── Dockerfile.internal
├── Dockerfile.worker
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

Todo upload grava, na mesma transação, uma análise `Pending` em `document_analyses`. O job `DocumentAnalysisWorker`, do processo `Jotanunes.Worker`, busca as pendentes no banco, lê o arquivo e produz um **parecer sugerido**: campos extraídos, achados e um veredito. **Não aprova nem recusa nada**: `approve`/`reject` continuam manuais, e o parecer serve de apoio ao analista.

**Cadeia de fallback.** Cada tipo de documento tem um analisador (`IDocumentTypeAnalyzer`, escolhido pelo `code` do tipo) que declara os campos obrigatórios. A leitura vai do mais barato ao mais caro e para no primeiro nível que lê todos os obrigatórios:

1. `NativeText`: camada de texto do PDF, via PdfPig. Grátis e instantâneo.
2. `Ocr`: Tesseract (`tesseract-ocr` + `tesseract-ocr-por`), pela linha de comando, sobre as páginas convertidas em imagem a 300 DPI. Grátis e roda no próprio worker. Lê documentos escaneados de layout fixo sem chamar a IA (hoje, a guia do FGTS) e serve de conferência para a leitura por imagem. Configuração em `Ocr`: `Enabled` (padrão `true`), `TesseractPath`, `Language` (`por`), `PageSegmentationMode` (6), `Dpi` (300), `MaxPages` (6) e `TimeoutSeconds` (60). Se o Tesseract não estiver instalado, o nível é pulado com um aviso no log.
3. `Vision`: modelo de IA com entrada de imagem, para analisadores que implementam `IVisionAnalyzer`. As páginas do PDF viram PNG (PDFtoImage/PDFium, até `DocumentImages:MaxPages`); imagens enviadas vão como estão.

CNPJ ou CPF com dígito verificador inválido conta como não lido, o que faz a leitura subir de nível. Se nenhum nível completa, a análise vai para `ManualReviewRequired` com a lista do que faltou.

**Papel da IA.** A IA só transcreve: recebe as imagens, as instruções e um JSON Schema do tipo de documento, e devolve os campos. Quem confere os campos com o cadastro é o mesmo código em C# usado para o texto. A IA faz três coisas:

- **Identifica o documento** (`isExpectedDocument`/`detectedDocument`). Um arquivo trocado (ex.: CNH enviada como CRF) gera o achado `WRONG_DOCUMENT_TYPE`.
- **Extrai os campos** de documentos escaneados ou sem layout fixo.
- **Responde perguntas visuais**, como "o recibo está assinado?".

Para não aceitar alucinação como fato: o schema permite `null` e as instruções mandam não deduzir; CPF, CNPJ e datas passam pelas mesmas validações do texto; e **todo achado de uma leitura por imagem fica no máximo como `Warning`**. Um `Blocking` exige leitura de texto.

**Conferência pelo OCR.** Quando o documento também passou pelo OCR, todo valor com 6 dígitos ou mais lido pela IA (CPF, CNPJ, valores, datas, competências) precisa aparecer no texto do OCR, comparando só os dígitos, de modo que `1.900,75` confere com `1900.75` e "Julho de 2026" com `07/2026`. Os que não aparecem geram o achado `VISION_NOT_CONFIRMED_BY_OCR` (Warning), com os valores. Valores parcialmente ocultos e listas ficam de fora, e a conferência não é feita quando o OCR leu menos de 20 dígitos na página.

**Provedor (OpenRouter).** A leitura por imagem usa o `OpenRouterVisionClient`, que chama `/chat/completions` com `response_format` em JSON Schema e `temperature: 0`. Toda requisição exige `provider.zdr: true`: o OpenRouter só usa provedores com retenção zero (no Gemini, o Vertex; o AI Studio fica de fora). Prompts e respostas não são gravados nos logs, só tokens e custo. Configuração no worker (`OpenRouter`): `Enabled` (padrão `false`), `ApiKey`, `Model` (padrão `google/gemini-3.8-flash`) e `TimeoutSeconds`. Sem `Enabled`, o nível `Vision` não existe e os documentos que dependem dele vão para revisão manual. A chave nunca vai para o `appsettings`:

```bash
dotnet user-secrets set "OpenRouter:ApiKey" "<chave>" --project Jotanunes.Worker
dotnet user-secrets set "OpenRouter:Enabled" "true" --project Jotanunes.Worker
```

No docker-compose, use as variáveis `OPENROUTER_ENABLED`, `OPENROUTER_API_KEY` e `OPENROUTER_MODEL`.

**Falhas e novas tentativas.** Cada falha agenda a próxima tentativa (`NextAttemptAt`) com espera crescente: 30 s, 1 min, 2 min... até 1 h. Erros temporários do provedor (429, 408, 5xx, timeout, rede) nunca levam a `Failed`. Os demais erros levam a `Failed` na 3ª tentativa.

| Status | Significado |
|---|---|
| `Pending` (1) | Na fila do worker |
| `Completed` (2) | Lido; ver `verdict` e `findings` |
| `ManualReviewRequired` (3) | Nenhum nível leu todos os campos obrigatórios; `failureReason` diz quais faltaram |
| `Failed` (4) | Erro técnico não temporário (ex.: PDF corrompido) em `MaxAttempts` (3) tentativas |
| `NotSupported` (5) | Ainda não há analisador para o código do tipo |

Veredito: `Conforming` (1) sem achados relevantes, `NeedsAttention` (2) com algum `Warning` e `NonConforming` (3) com algum `Blocking`. Achados `Info` só informam (ex.: validade identificada na certidão).

Analisadores disponíveis:

| Código do tipo | Documento | Leitura | Confere |
|---|---|---|---|
| `FGTS_CND` (id 4, "Certidão Negativa de FGTS") | Certificado de Regularidade do FGTS (CRF) | Texto; imagem se escaneada | CNPJ igual ao da empresa; se está vencida, se ainda não vale ou se vence em até 7 dias; validade informada no envio × certidão; razão social |
| `PAYMENT_RECEIPT` (id 20, "Recibo de Pagamento") | Recibo de pagamento (holerite) | Imagem | CNPJ do empregador; CPF e nome do trabalhador do envio; competência dentro do período do envio; assinatura do empregado. Guarda o líquido (`netPay`) para as regras cruzadas |
| `PAYMENT_PROOF` (id 19, "Comprovante de Pagamento") | Comprovante de pagamento do salário (PIX/TED) | Texto; imagem para outros bancos | CPF de quem recebeu × trabalhador do envio (aceita CPF parcialmente oculto, conferindo os dígitos visíveis); nome; CNPJ pagador × empresa; pagamento até o 5º dia útil do mês seguinte à competência (sábado conta como dia útil; domingos e feriados nacionais fixos não). Guarda `amount` para as regras cruzadas |
| `FGTS_PAYMENT_PROOF` (id 12) | Comprovante de pagamento da guia do FGTS | Texto; imagem | CNPJ pagador × empresa (raiz); recebedor é a Caixa. Guarda `amount` e `paymentDate` |
| `FGTS_DETAIL` (id 13) | Detalhamento da guia do FGTS Digital | Texto; imagem | Raiz do CNPJ do empregador; competência no período; quantidade declarada × trabalhadores listados. Guarda `workers` (nome, CPF, base, FGTS), `guideTotal`, `dueDate` e `tomadorCnpj` |
| `FGTS_REPORT` (id 14, "Relatório do FGTS") | Guia do FGTS Digital (GFD) | Imagem | Raiz do CNPJ do empregador; competência no período. Guarda `guideTotal`, `dueDate`, `declaredWorkers` |
| `TIMESHEET` (id 18) | Folha de ponto | Texto; imagem | CPF e nome × trabalhador; período; CNPJ do empregador (ausente gera Warning); 0:00 trabalhadas; local × obra da solicitação. Guarda `workedHours` e `expectedHours` |
| `EMPLOYEE_LIST` (id 17) | Relação de funcionários lotados na obra | Texto (uma linha por funcionário); imagem | Obra listada × obra da solicitação. Guarda `employees` |

Campos de lista (`workers`, `employees`) ficam em `fields` como JSON. Valores em reais ficam com ponto decimal (`1900.75`), datas em `dd/MM/yyyy`, competências em `MM/yyyy`, horas em `H:mm` e CPF parcialmente oculto com `*` nas posições ocultas (`***952844**`).

Endpoints (frente interna):

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/document/{id}/analysis` | Parecer do documento |
| POST | `/api/document/{id}/analysis` | Recoloca a análise na fila (202). Também cria a análise de documentos enviados antes desta funcionalidade |

**Validade automática.** Para tipos cujo analisador lê a validade (hoje, a CRF), uma leitura de **texto** ajusta o `expirationDate` do documento enquanto ele está pendente: preenche quando o fornecedor não informou (achado `EXPIRATION_DATE_FILLED`, Info) e corrige quando ele informou outra data (achado `EXPIRATION_DATE_CORRECTED`, Warning, com a data original na mensagem). Uma leitura **por imagem** nunca altera a data, só sugere (`EXPIRATION_DATE_MISMATCH`). Documento já aprovado ou recusado não é alterado.

**Aviso ao fornecedor.** Quando a análise conclui que o arquivo é outro documento (`WRONG_DOCUMENT_TYPE`) e o documento ainda está pendente, o fornecedor recebe um e-mail ("Confira o documento enviado"), e o documento passa a trazer `uploadWarning` nas duas frentes. É o único sinal da análise que o fornecedor vê: a mensagem é fixa e não repete o que a IA acha que o arquivo é. O worker usa a mesma seção `Email` das APIs.

**Métricas de concordância.** `GET /api/document/analysis/metrics?from=2026-07-01&to=2026-07-31` (frente interna) compara o veredito da análise com a decisão do analista nos documentos avaliados no período, no total e por tipo:

| Campo | Significado |
|---|---|
| `agreements` | `Conforming` aprovado ou `NonConforming` recusado |
| `falseAlarms` | `NonConforming` aprovado: a análise apontou um problema que o analista não confirmou |
| `missedProblems` | `Conforming` recusado: a análise deixou passar algo |
| `attentionApproved` / `attentionRejected` | `NeedsAttention`, separado por decisão |
| `notAnalyzed` | Sem análise concluída (tipo sem analisador, revisão manual, falha) |
| `agreementRate` | `agreements / (agreements + falseAlarms + missedProblems)`, ou `null` sem base |

**Resumo na listagem (só na frente interna).** `GET /api/document` e `GET /api/document/{id}` da frente interna (e as respostas de `approve`/`reject`) trazem o campo `analysis` com `status`, `verdict`, `blockingCount`, `warningCount` e `analyzedAt`. Ele vem `null` quando o documento ainda não tem análise. A listagem aceita os filtros `analysisStatus` e `analysisVerdict` (ex.: `?analysisVerdict=3` lista só os não conformes). O documento aparece para a equipe assim que é enviado, e aprovar ou recusar **não depende da análise**: o resumo é apoio, não trava. A frente externa usa outro DTO e outro filtro, sem esses campos: o fornecedor não recebe o parecer nem consegue filtrar por ele.

### Cruzamento dos documentos do período

A análise de cada documento confere o documento sozinho. O **cruzamento** compara os documentos de uma solicitação dentro de um período (`PeriodComplianceReport`, tabela `period_compliance_reports`, um por solicitação e período) com os trabalhadores alocados na obra naquele período.

**Quando é recalculado.** O relatório do período é colocado na fila (`Pending`) quando a análise de um documento recorrente termina, quando um documento recorrente é recusado e quando um trabalhador é alocado, liberado ou desativado (nesse caso, todos os relatórios da solicitação que terminam a partir de hoje). O job `PeriodComplianceWorker` do `Jotanunes.Worker` recalcula os pendentes (`Schedules:PeriodComplianceCron`, padrão `*/30 * * * * *`; `PeriodCompliance:BatchSize`, padrão 10). Se um novo pedido chega durante o cálculo, o relatório volta para a fila.

**O que entra.** Documentos da solicitação cujo período de referência alcança o período do relatório, exceto os recusados. Só entram na comparação documentos com análise concluída que não foram identificados como outro documento; os demais aparecem no achado `UNREAD_DOCUMENTS` (Info). Quando há mais de um documento do mesmo tipo e trabalhador, vale o mais recente; comprovantes de salário e de FGTS são somados, para aceitar pagamentos divididos. Trabalhadores alocados são os com alocação ativa em algum dia do período.

| Achado | Gravidade | Regra |
|---|---|---|
| `PAYMENT_AMOUNT_MISMATCH` | Blocking* | Líquido do recibo diferente da soma dos comprovantes de salário do mesmo trabalhador (tolerância de R$ 0,01) |
| `PAID_WITHOUT_WORKED_HOURS` | Warning | Recibo com líquido maior que zero e folha de ponto com 0:00 trabalhadas |
| `WORKER_WITHOUT_FGTS` | Blocking* | Trabalhador alocado que não aparece (pelo CPF) no detalhamento do FGTS |
| `FGTS_PAYMENT_MISMATCH` | Blocking* | Total da guia (GFD, ou o detalhamento se não houver guia) diferente da soma dos comprovantes de FGTS |
| `FGTS_PAID_LATE` | Warning | Comprovante de FGTS pago depois do vencimento da guia |
| `FGTS_FEWER_WORKERS_THAN_ALLOCATED` | Warning | Guia declara menos trabalhadores que os alocados na obra |
| `ALLOCATED_NOT_IN_EMPLOYEE_LIST` / `LISTED_NOT_ALLOCATED` | Warning | Relação de funcionários × alocações (pelo CPF, ou pelo nome quando a relação não traz CPF) |
| `UNREAD_DOCUMENTS` | Info | Documentos ainda sem leitura, fora do cruzamento |

\* Blocking só quando todos os documentos envolvidos foram lidos por texto; se algum foi lido por imagem, vira Warning, pela mesma regra da análise de documentos.

Endpoints (frente interna):

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/supplyrequest/{id}/compliance-reports` | Relatórios de cruzamento da solicitação, do período mais recente para o mais antigo |
| POST | `/api/supplyrequest/{id}/compliance-reports` | Coloca o período (`periodStart`, `periodEnd`) na fila de recálculo (202) |

**Worker.** `Jotanunes.Worker` é um processo próprio (SDK `Microsoft.NET.Sdk.Worker`), separado das duas APIs, com um job agendado por cron para cada tarefa de fundo:

- Cada job herda de `CronBackgroundService` e recebe um `ICronSchedule` próprio (`Schedules/`). A cada disparo, o job abre um escopo de DI e chama o serviço da aplicação. Um disparo só começa depois que o anterior termina.
- `DocumentAnalysisWorker` esvazia a fila em lotes de `DocumentAnalysis:BatchSize` (10), com um escopo por análise para uma falha não afetar as demais. Se alguma análise do lote não puder ser gravada, ele para e espera o próximo disparo.
- As crons ficam na seção `Schedules` e são validadas na subida: sem cron, o processo não sobe. `DocumentAnalysisCron` aceita 5 campos (padrão) ou 6 (com segundos); o padrão é `*/10 * * * * *`. Use `-` ou `never` para desligar um job.
- A fila não trava as linhas que está processando, então **só uma instância do worker pode rodar por vez**.
- O worker não aplica migrations: o schema continua sendo da API interna.

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

Sobe o SQL Server, a API interna na porta `8080`, a API externa na porta `80` e o worker (sem porta). As migrations são aplicadas automaticamente pela API interna, que é a dona do schema.

Swagger:

- Interna: http://localhost:8080/swagger
- Externa: http://localhost/swagger

---

## Executando com Aspire

O `Jotanunes.AppHost` (.NET Aspire 13, exige o SDK do .NET 10) sobe o SQL Server em container, cria as bases `jotanunes_portal` (dev), `jotanunes_portal_test_external` e `jotanunes_portal_test_internal` (uma para cada projeto `*.BddTests`), aplica as migrations do EF Core e inicia as duas APIs e o worker já apontando para a base de dev. O dashboard mostra o status, os logs e o health de cada recurso.

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
dotnet run --project Jotanunes.Worker         # jobs agendados (análise automática)
```

O worker usa o Tesseract para o OCR. No macOS, instale com `brew install tesseract tesseract-lang`; a imagem Docker do worker já vem com ele.

A connection string vem da variável de ambiente `DATABASE` ou, na falta dela, de `ConnectionStrings:ConnectionString` no `appsettings.json`.

---

## Migrations

```bash
dotnet ef migrations add NomeDaMigration --project Jotanunes.Infra.Data --startup-project Jotanunes.API.Internal
dotnet ef database update --project Jotanunes.Infra.Data --startup-project Jotanunes.API.Internal
```

---

## Testes

São quatro projetos:

| Projeto | Tipo | Depende de banco |
| --- | --- | --- |
| `Jotanunes.Tests` | Unitários (xUnit + Moq), organizados em pastas que espelham as camadas: `Domain/`, `Application/`, `Infra/` | Não |
| `Jotanunes.Worker.Tests` | Unitários (xUnit v3 + Moq) dos jobs e agendamentos do worker | Não |
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

**Golden set da análise automática.** Os analisadores são conferidos contra documentos reais anotados em `Documentos Jotanunes/golden-set.json`. A pasta fica fora do git porque os arquivos têm dados pessoais; sem ela, os testes passam sem conferir nada. Cada caso informa o arquivo, o código do tipo, os campos esperados (`expected`) e, para documentos errados de propósito, `expectComplete: false` (leitura de texto) ou `expectWrongDocument: true` (leitura por imagem). Ao escrever um analisador novo, anote primeiro os arquivos reais dele aqui.

- `GoldenSetTests` roda os casos de texto. É grátis e roda sempre.
- `OcrGoldenSetTests` roda os casos com `"engine": "ocr"` com o Tesseract de verdade. É grátis, mas só confere quando o Tesseract está instalado; sem ele, passa sem conferir. No macOS: `brew install tesseract tesseract-lang`.
- `VisionGoldenSetTests` roda os casos com `"engine": "vision"` contra o modelo de verdade. É um **teste de avaliação** (categoria `Eval`), não um teste unitário: mede a precisão do modelo e pode falhar sem bug no código, se o modelo errar um campo. **Gasta créditos do OpenRouter**, então só roda quando pedido. Rode num terminal fora de qualquer ferramenta que grave histórico, para a chave não ficar exposta:

```bash
JOTANUNES_VISION_GOLDEN=1 OPENROUTER_API_KEY=<chave> dotnet test Jotanunes.Tests --filter Category=Eval --logger "console;verbosity=detailed"
```

Para rodar a bateria normal sem a avaliação, use `--filter Category!=Eval`. Sem `JOTANUNES_VISION_GOLDEN=1`, a avaliação passa sem chamar o modelo.

A saída mostra, por arquivo, se conferiu, quantas páginas foram enviadas, o tempo e o JSON devolvido. `OPENROUTER_MODEL` troca o modelo, para comparar opções (ex.: `google/gemini-3.5-flash-lite`).

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
