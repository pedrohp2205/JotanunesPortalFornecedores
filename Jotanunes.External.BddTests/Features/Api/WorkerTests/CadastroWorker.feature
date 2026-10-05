# language: pt-BR
Funcionalidade: Worker - Cadastro

Como fornecedor autenticado
Quero manter o cadastro dos trabalhadores da minha empresa
Para alocá-los nas obras e enviar os documentos deles sem redigitar nome e CPF

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81" que fornece mão de obra
    E que existe outra empresa cadastrada
    E que eu estou autenticado com senha definitiva

Cenário: Cadastrar um trabalhador
    Quando eu cadastrar o trabalhador "José da Silva" com CPF "529.982.247-25"
    Então eu recebo uma resposta 200 OK
    E o trabalhador retornado deve estar ativo com CPF "52998224725"

Cenário: Não permitir cadastrar o mesmo CPF duas vezes na empresa
    Dado que existe um trabalhador "José da Silva" com CPF "529.982.247-25" cadastrado para a empresa
    Quando eu cadastrar o trabalhador "José S." com CPF "52998224725"
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Já existe um trabalhador cadastrado com este CPF."

Cenário: Não permitir cadastrar trabalhador com CPF inválido
    Quando eu cadastrar o trabalhador "José da Silva" com CPF "111.111.111-11"
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "CPF do trabalhador inválido."

Cenário: Listar apenas os trabalhadores da minha empresa
    Dado que existe um trabalhador cadastrado para a empresa
    E que existe um trabalhador cadastrado para a outra empresa
    Quando eu listar os trabalhadores da minha empresa
    Então eu recebo uma resposta 200 OK
    E a listagem de trabalhadores contém apenas o trabalhador da minha empresa

Cenário: Atualizar o nome de um trabalhador
    Dado que existe um trabalhador cadastrado para a empresa
    Quando eu alterar o nome do trabalhador cadastrado para "José da Silva Santos"
    Então eu recebo uma resposta 200 OK
    E o trabalhador retornado deve se chamar "José da Silva Santos"

Cenário: Desativar um trabalhador alocado o retira da obra
    Dado que existe uma obra cadastrada
    E que existe uma solicitação de mão de obra aberta para a empresa e a obra cadastradas
    E que existe um trabalhador cadastrado para a empresa
    E que o trabalhador cadastrado está alocado na solicitação cadastrada
    Quando eu desativar o trabalhador cadastrado
    Então eu recebo uma resposta 200 OK
    E o trabalhador retornado deve estar inativo
    E a solicitação cadastrada não deve ter trabalhadores alocados

Cenário: Não permitir consultar trabalhador de outra empresa
    Dado que existe um trabalhador cadastrado para a outra empresa
    Quando eu consultar o trabalhador da outra empresa
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Trabalhador não encontrado"
