# language: pt-BR
Funcionalidade: DocumentType - Exclusão

Como membro da equipe da Jotanunes
Quero desativar um tipo de documento
Para que ele deixe de ser exigido sem perder o histórico de envios

Cenário: Desativar um tipo de documento
    Dado que existe um tipo de documento cadastrado com código "ALVARA"
    Quando eu excluir o tipo de documento cadastrado
    Então eu recebo uma resposta 200 OK
    E o tipo de documento retornado deve estar inativo

Cenário: Excluir um tipo de documento inexistente
    Quando eu excluir um tipo de documento inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Tipo de documento não encontrado"
