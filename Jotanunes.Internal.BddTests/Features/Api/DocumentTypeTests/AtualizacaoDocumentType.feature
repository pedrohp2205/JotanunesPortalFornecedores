# language: pt-BR
Funcionalidade: DocumentType - Atualização

Como membro da equipe da Jotanunes
Quero atualizar um tipo de documento
Para ajustar o nome e as regras de aplicação

Contexto:
    Dado que existe um tipo de documento cadastrado com código "ALVARA"

Cenário: Atualizar um tipo de documento
    Quando eu atualizar o tipo de documento cadastrado com o nome "Alvará Municipal"
    Então eu recebo uma resposta 200 OK
    E o tipo de documento retornado deve ter o nome "Alvará Municipal" e manter o código "ALVARA"

Cenário: Não permitir tornar o documento condicional sem a descrição da condição
    Quando eu tornar o tipo de documento cadastrado condicional sem descrever a condição
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Descrição da condição é obrigatória quando o documento é condicional."

Cenário: Atualizar um tipo de documento inexistente
    Quando eu atualizar um tipo de documento inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Tipo de documento não encontrado"
