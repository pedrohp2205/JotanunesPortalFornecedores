# language: pt-BR
Funcionalidade: Worker - Leitura e alocação

Como membro da equipe da Jotanunes
Quero consultar os trabalhadores dos fornecedores e quem está alocado em cada obra
Para controlar as pessoas presentes nas obras

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81" que fornece mão de obra
    E que existe outra empresa cadastrada
    E que existe uma obra cadastrada
    E que existe uma solicitação de mão de obra aberta para a empresa e a obra cadastradas
    E que existe um trabalhador cadastrado para a empresa
    E que existe um trabalhador cadastrado para a outra empresa

Cenário: Filtrar os trabalhadores de uma empresa
    Quando eu listar os trabalhadores da empresa cadastrada
    Então eu recebo uma resposta 200 OK
    E a listagem de trabalhadores contém apenas o trabalhador da empresa cadastrada

Cenário: Consultar um trabalhador
    Quando eu consultar o trabalhador cadastrado
    Então eu recebo uma resposta 200 OK
    E o trabalhador retornado deve ser o trabalhador cadastrado

Cenário: Consultar os trabalhadores alocados numa solicitação
    Dado que o trabalhador cadastrado está alocado na solicitação cadastrada
    Quando eu listar os trabalhadores alocados na solicitação cadastrada
    Então eu recebo uma resposta 200 OK
    E a listagem de alocados contém apenas o trabalhador cadastrado

Cenário: Desalocar um trabalhador de uma solicitação
    Dado que o trabalhador cadastrado está alocado na solicitação cadastrada
    Quando eu desalocar o trabalhador cadastrado da solicitação cadastrada
    Então eu recebo uma resposta 200 OK
    E a solicitação cadastrada não deve ter trabalhadores alocados

Cenário: Não permitir desalocar trabalhador que não está alocado
    Quando eu desalocar o trabalhador cadastrado da solicitação cadastrada
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Trabalhador não está alocado nesta solicitação"

Cenário: Consultar um trabalhador inexistente
    Quando eu consultar um trabalhador inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Trabalhador não encontrado"
