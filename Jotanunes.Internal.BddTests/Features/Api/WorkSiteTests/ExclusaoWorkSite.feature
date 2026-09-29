# language: pt-BR
Funcionalidade: WorkSite - Exclusão

Como membro da equipe da Jotanunes
Quero excluir uma obra
Para que ela deixe de aparecer nas consultas

Cenário: Excluir uma obra
    Dado que existe uma obra cadastrada
    Quando eu excluir a obra cadastrada
    Então eu recebo uma resposta 200 OK
    E a obra cadastrada não deve mais ser encontrada

Cenário: Excluir uma obra inexistente
    Quando eu excluir uma obra inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Obra não encontrada"
