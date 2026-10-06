# language: pt-BR
Funcionalidade: SupplyRequest - Cruzamento de documentos do período

Como membro da equipe da Jotanunes
Quero pedir e consultar o cruzamento dos documentos de cada período de uma solicitação
Para ver inconsistências entre recibos, pagamentos, folhas de ponto e FGTS

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que existe uma obra cadastrada
    E que existe uma solicitação de mão de obra aberta para a empresa e a obra cadastradas

Cenário: Pedir o cruzamento de um período
    Quando eu pedir o cruzamento da solicitação cadastrada para o período de "2026-07-01" a "2026-07-31"
    Então eu recebo uma resposta 202 Accepted
    E o relatório de cruzamento retornado está na fila para o período de "2026-07-01" a "2026-07-31"

Cenário: Listar os cruzamentos da solicitação
    Dado que foi pedido o cruzamento da solicitação cadastrada para o período de "2026-07-01" a "2026-07-31"
    Quando eu listar os cruzamentos da solicitação cadastrada
    Então eu recebo uma resposta 200 OK
    E a listagem de cruzamentos tem 1 relatório(s)

Cenário: Pedir o mesmo cruzamento duas vezes reaproveita o relatório
    Dado que foi pedido o cruzamento da solicitação cadastrada para o período de "2026-07-01" a "2026-07-31"
    E que foi pedido o cruzamento da solicitação cadastrada para o período de "2026-07-01" a "2026-07-31"
    Quando eu listar os cruzamentos da solicitação cadastrada
    Então eu recebo uma resposta 200 OK
    E a listagem de cruzamentos tem 1 relatório(s)

Cenário: Listar os cruzamentos de uma solicitação inexistente
    Quando eu listar os cruzamentos de uma solicitação inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Solicitação não encontrada"

Cenário: Pedir o cruzamento de uma solicitação inexistente
    Quando eu pedir o cruzamento de uma solicitação inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Solicitação não encontrada"
