# language: pt-BR
Funcionalidade: SupplyRequest - Atualização

Como membro da equipe da Jotanunes
Quero ajustar a quantidade de trabalhadores exigida em uma solicitação de mão de obra
Para acompanhar a meta de trabalhadores em dia

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81" que fornece material e mão de obra
    E que existe uma obra cadastrada

Cenário: Atualizar a quantidade de trabalhadores de uma solicitação de mão de obra
    Dado que existe uma solicitação de mão de obra aberta para a empresa e a obra cadastradas
    Quando eu atualizar a quantidade de trabalhadores da solicitação cadastrada para 5
    Então eu recebo uma resposta 200 OK
    E a solicitação retornada deve exigir 5 trabalhadores

Cenário: Não permitir informar trabalhadores em solicitação de material
    Dado que existe uma solicitação de material aberta para a empresa e a obra cadastradas
    Quando eu atualizar a quantidade de trabalhadores da solicitação cadastrada para 5
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Quantidade de trabalhadores necessária só se aplica a solicitações de mão de obra."

Cenário: Não permitir alterar uma solicitação encerrada
    Dado que existe uma solicitação de mão de obra cancelada para a empresa e a obra cadastradas
    Quando eu atualizar a quantidade de trabalhadores da solicitação cadastrada para 5
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Solicitação encerrada não aceita alterações nem novos documentos."
