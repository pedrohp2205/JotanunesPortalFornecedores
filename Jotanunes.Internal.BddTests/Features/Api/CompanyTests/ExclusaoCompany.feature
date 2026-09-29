# language: pt-BR
Funcionalidade: Company - Exclusão

Como membro da equipe da Jotanunes
Quero excluir uma empresa fornecedora
Para que ela deixe de aparecer nas consultas

Cenário: Excluir uma empresa
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    Quando eu excluir a empresa cadastrada
    Então eu recebo uma resposta 200 OK
    E a empresa cadastrada não deve mais ser encontrada

Cenário: Excluir uma empresa inexistente
    Quando eu excluir uma empresa inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Empresa não encontrada"
