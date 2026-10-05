# language: pt-BR
Funcionalidade: Worker - Alocação

Como fornecedor autenticado
Quero informar quais trabalhadores estão alocados em cada obra
Para que a Jotanunes saiba quem está na obra e cobre os documentos de cada um

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81" que fornece material e mão de obra
    E que existe outra empresa cadastrada
    E que existe uma obra cadastrada
    E que eu estou autenticado com senha definitiva

Cenário: Alocar um trabalhador numa solicitação de mão de obra
    Dado que existe uma solicitação de mão de obra aberta para a empresa e a obra cadastradas
    E que existe um trabalhador cadastrado para a empresa
    Quando eu alocar o trabalhador cadastrado na solicitação cadastrada
    Então eu recebo uma resposta 200 OK
    E a solicitação cadastrada deve ter apenas o trabalhador cadastrado alocado

Cenário: Não permitir alocar o mesmo trabalhador duas vezes
    Dado que existe uma solicitação de mão de obra aberta para a empresa e a obra cadastradas
    E que existe um trabalhador cadastrado para a empresa
    E que o trabalhador cadastrado está alocado na solicitação cadastrada
    Quando eu alocar o trabalhador cadastrado na solicitação cadastrada
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Trabalhador já está alocado nesta solicitação."

Cenário: Não permitir alocar além da quantidade de trabalhadores necessária
    Dado que existe uma solicitação de mão de obra aberta para a empresa e a obra cadastradas
    E que a solicitação cadastrada já tem todos os trabalhadores necessários alocados
    E que existe um trabalhador cadastrado para a empresa
    Quando eu alocar o trabalhador cadastrado na solicitação cadastrada
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "A solicitação já tem os 3 trabalhadores necessários alocados. Desaloque um antes de alocar outro."

Cenário: Não permitir alocar trabalhador em solicitação de material
    Dado que existe uma solicitação de material aberta para a empresa e a obra cadastradas
    E que existe um trabalhador cadastrado para a empresa
    Quando eu alocar o trabalhador cadastrado na solicitação cadastrada
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Só é possível alocar trabalhadores em solicitações de mão de obra."

Cenário: Não permitir alocar trabalhador de outra empresa
    Dado que existe uma solicitação de mão de obra aberta para a empresa e a obra cadastradas
    E que existe um trabalhador cadastrado para a outra empresa
    Quando eu alocar o trabalhador da outra empresa na solicitação cadastrada
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Trabalhador não encontrado"

Cenário: Desalocar um trabalhador mantém o histórico
    Dado que existe uma solicitação de mão de obra aberta para a empresa e a obra cadastradas
    E que existe um trabalhador cadastrado para a empresa
    E que o trabalhador cadastrado está alocado na solicitação cadastrada
    Quando eu desalocar o trabalhador cadastrado da solicitação cadastrada
    Então eu recebo uma resposta 200 OK
    E a solicitação cadastrada não deve ter trabalhadores alocados
    E o histórico da solicitação cadastrada deve mostrar o trabalhador cadastrado desalocado
