# language: pt-BR
Funcionalidade: WorkSite - Leitura

Como membro da equipe da Jotanunes
Quero consultar as obras cadastradas
Para escolher a obra ao abrir uma solicitação

Contexto:
    Dado que existe uma obra cadastrada

Cenário: Listar as obras
    Quando eu listar as obras
    Então eu recebo uma resposta 200 OK
    E a listagem de obras contém a obra cadastrada

Cenário: Consultar uma obra pelo identificador
    Quando eu consultar a obra cadastrada
    Então eu recebo uma resposta 200 OK
    E a obra retornada deve ser a obra cadastrada

Cenário: Consultar uma obra inexistente
    Quando eu consultar uma obra inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Obra não encontrada"
