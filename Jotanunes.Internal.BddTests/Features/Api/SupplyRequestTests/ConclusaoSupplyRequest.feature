# language: pt-BR
Funcionalidade: SupplyRequest - Conclusão

Como membro da equipe da Jotanunes
Quero concluir uma solicitação quando o fornecedor entregou toda a documentação
Para encerrar o acompanhamento daquele fornecimento

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que existe uma obra cadastrada

Cenário: Concluir uma solicitação sem pendências
    Dado que existe uma solicitação de material aberta para a empresa e a obra cadastradas
    E que a empresa cadastrada tem todos os documentos de habilitação obrigatórios aprovados
    Quando eu concluir a solicitação cadastrada
    Então eu recebo uma resposta 200 OK
    E a solicitação retornada deve estar concluída

Cenário: Não permitir concluir uma solicitação com pendências
    Dado que existe uma solicitação de material aberta para a empresa e a obra cadastradas
    Quando eu concluir a solicitação cadastrada
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro contendo a mensagem "Não é possível concluir a solicitação: faltam 6 documentos de habilitação"

Cenário: Não permitir concluir uma solicitação já encerrada
    Dado que existe uma solicitação de material cancelada para a empresa e a obra cadastradas
    Quando eu concluir a solicitação cadastrada
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Solicitação encerrada não aceita alterações nem novos documentos."
