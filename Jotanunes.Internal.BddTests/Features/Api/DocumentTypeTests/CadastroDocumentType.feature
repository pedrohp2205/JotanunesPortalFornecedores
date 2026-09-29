# language: pt-BR
Funcionalidade: DocumentType - Cadastro

Como membro da equipe da Jotanunes
Quero cadastrar os tipos de documento exigidos dos fornecedores
Para montar o checklist de conformidade

Cenário: Cadastrar um tipo de documento
    Quando eu cadastrar o tipo de documento com código "alvara_funcionamento"
    Então eu recebo uma resposta 200 OK
    E o tipo de documento retornado deve ter o código "ALVARA_FUNCIONAMENTO" e estar ativo

Cenário: Não permitir cadastrar tipo de documento com código já existente
    Quando eu cadastrar o tipo de documento com código "CNPJ_CARD"
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Já existe um tipo de documento com este código."

Cenário: Não permitir cadastrar documento condicional sem a descrição da condição
    Quando eu cadastrar o tipo de documento condicional com código "LAUDO_TECNICO" sem descrever a condição
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Descrição da condição é obrigatória quando o documento é condicional."
