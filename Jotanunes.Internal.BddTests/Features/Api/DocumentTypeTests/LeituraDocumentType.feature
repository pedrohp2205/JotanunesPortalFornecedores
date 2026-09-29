# language: pt-BR
Funcionalidade: DocumentType - Leitura

Como membro da equipe da Jotanunes
Quero consultar os tipos de documento cadastrados
Para saber o que é exigido de cada fornecedor

Cenário: Listar os tipos de documento
    Quando eu listar os tipos de documento
    Então eu recebo uma resposta 200 OK
    E a listagem de tipos de documento contém os tipos padrão do sistema

Cenário: Consultar um tipo de documento pelo identificador
    Quando eu consultar o tipo de documento Cartão de CNPJ
    Então eu recebo uma resposta 200 OK
    E o tipo de documento retornado deve ter o código "CNPJ_CARD" e estar ativo

Cenário: Consultar um tipo de documento inexistente
    Quando eu consultar um tipo de documento inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Tipo de documento não encontrado"
