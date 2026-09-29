# language: pt-BR
Funcionalidade: SupplyRequest - Leitura

Como membro da equipe da Jotanunes
Quero consultar as solicitações de fornecimento
Para acompanhar o que cada fornecedor ainda precisa entregar

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que existe uma obra cadastrada
    E que existe uma solicitação de material aberta para a empresa e a obra cadastradas

Cenário: Listar as solicitações da empresa
    Quando eu listar as solicitações da empresa cadastrada
    Então eu recebo uma resposta 200 OK
    E a listagem de solicitações contém a solicitação cadastrada

Cenário: Consultar uma solicitação com as pendências de documentação
    Quando eu consultar a solicitação cadastrada
    Então eu recebo uma resposta 200 OK
    E a solicitação retornada deve indicar 6 documentos de habilitação pendentes

Cenário: Consultar uma solicitação inexistente
    Quando eu consultar uma solicitação inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Solicitação não encontrada"
