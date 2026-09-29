# language: pt-BR
Funcionalidade: SupplierUser - Redefinição de senha pela equipe

Como membro da equipe da Jotanunes
Quero definir uma senha provisória para um acesso
Para destravar um fornecedor que perdeu a senha

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que existe um usuário de acesso "maria@alfa.com.br" ativo para a empresa cadastrada

Cenário: Definir uma senha provisória
    Quando eu redefinir a senha do acesso cadastrado para "Provisoria@2"
    Então eu recebo uma resposta 200 OK
    E o acesso retornado deve estar ativo e exigir a troca de senha

Cenário: Não permitir senha provisória curta
    Quando eu redefinir a senha do acesso cadastrado para "curta"
    Então eu recebo uma resposta 400 Bad Request

Cenário: Redefinir a senha de um acesso de outra empresa
    Dado que existe outra empresa cadastrada
    E que existe um usuário de acesso "joao@beta.com.br" para a outra empresa
    Quando eu redefinir a senha do acesso da outra empresa pela empresa cadastrada
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Usuário não encontrado"
