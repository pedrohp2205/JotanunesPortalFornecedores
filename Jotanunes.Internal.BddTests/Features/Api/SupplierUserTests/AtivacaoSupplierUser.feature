# language: pt-BR
Funcionalidade: SupplierUser - Ativação e desativação

Como membro da equipe da Jotanunes
Quero ativar e desativar os acessos ao portal
Para controlar quem pode entrar em nome da empresa

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"

Cenário: Desativar um acesso ativo
    Dado que existe um usuário de acesso "maria@alfa.com.br" ativo para a empresa cadastrada
    Quando eu desativar o acesso cadastrado
    Então eu recebo uma resposta 200 OK
    E o acesso retornado deve estar inativo

Cenário: Reativar um acesso inativo
    Dado que existe um usuário de acesso "maria@alfa.com.br" inativo para a empresa cadastrada
    Quando eu ativar o acesso cadastrado
    Então eu recebo uma resposta 200 OK
    E o acesso retornado deve estar ativo

Esquema do Cenário: Não permitir alterar o acesso de outra empresa
    Dado que existe outra empresa cadastrada
    E que existe um usuário de acesso "joao@beta.com.br" para a outra empresa
    Quando eu <acao> o acesso da outra empresa pela empresa cadastrada
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Usuário não encontrado"

    Exemplos:
        | acao      |
        | ativar    |
        | desativar |
