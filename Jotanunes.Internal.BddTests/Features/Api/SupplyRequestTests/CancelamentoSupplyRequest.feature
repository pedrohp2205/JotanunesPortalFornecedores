# language: pt-BR
Funcionalidade: SupplyRequest - Cancelamento

Como membro da equipe da Jotanunes
Quero cancelar uma solicitação de fornecimento
Para encerrá-la mesmo que ainda existam pendências

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que existe uma obra cadastrada

Cenário: Cancelar uma solicitação com pendências
    Dado que existe uma solicitação de material aberta para a empresa e a obra cadastradas
    Quando eu cancelar a solicitação cadastrada
    Então eu recebo uma resposta 200 OK
    E a solicitação retornada deve estar cancelada

Cenário: Não permitir cancelar uma solicitação já concluída
    Dado que existe uma solicitação de material concluída para a empresa e a obra cadastradas
    Quando eu cancelar a solicitação cadastrada
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Solicitação encerrada não aceita alterações nem novos documentos."

Cenário: Cancelar uma solicitação inexistente
    Quando eu cancelar uma solicitação inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Solicitação não encontrada"
