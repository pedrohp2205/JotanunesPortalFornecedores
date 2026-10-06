# language: pt-BR
Funcionalidade: Document - Leitura

Como membro da equipe da Jotanunes
Quero consultar os documentos enviados pelos fornecedores
Para avaliá-los

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que existe um documento de habilitação pendente enviado pela empresa cadastrada

Cenário: Listar os documentos da empresa
    Quando eu listar os documentos da empresa cadastrada
    Então eu recebo uma resposta 200 OK
    E a listagem de documentos contém o documento cadastrado

Cenário: Consultar um documento pelo identificador
    Quando eu consultar o documento cadastrado
    Então eu recebo uma resposta 200 OK
    E o documento retornado deve ser o Cartão de CNPJ da empresa cadastrada

Cenário: Consultar um documento inexistente
    Quando eu consultar um documento inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Documento não encontrado"

Cenário: Listar os documentos com o resumo da análise automática
    Dado que o documento cadastrado tem uma análise não conforme
    Quando eu listar os documentos da empresa cadastrada
    Então eu recebo uma resposta 200 OK
    E o documento listado mostra a análise com veredito "NonConforming" e 1 apontamento(s) bloqueante(s)

Cenário: Consultar um documento que ainda não foi analisado
    Quando eu consultar o documento cadastrado
    Então eu recebo uma resposta 200 OK
    E o documento retornado não tem resumo da análise

Cenário: Filtrar os documentos pelo veredito da análise
    Dado que o documento cadastrado tem uma análise não conforme
    Quando eu listar os documentos da empresa cadastrada com veredito da análise 3
    Então eu recebo uma resposta 200 OK
    E a listagem de documentos contém o documento cadastrado

Cenário: Filtrar os documentos por um veredito que nenhum documento tem
    Dado que o documento cadastrado tem uma análise não conforme
    Quando eu listar os documentos da empresa cadastrada com veredito da análise 1
    Então eu recebo uma resposta 200 OK
    E a listagem de documentos está vazia
