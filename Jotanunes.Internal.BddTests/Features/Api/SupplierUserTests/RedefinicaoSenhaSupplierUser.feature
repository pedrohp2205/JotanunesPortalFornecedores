# language: pt-BR
Funcionalidade: SupplierUser - Redefinição de senha pela equipe

Como membro da equipe da Jotanunes
Quero redefinir a senha de um acesso
Para destravar um fornecedor que perdeu a senha

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que existe um usuário de acesso "maria@alfa.com.br" ativo para a empresa cadastrada

Cenário: Redefinir a senha de um acesso
    Quando eu redefinir a senha do acesso cadastrado
    Então eu recebo uma resposta 200 OK
    E o acesso retornado deve estar ativo e exigir a troca de senha

Cenário: Redefinir a senha de um acesso de outra empresa
    Dado que existe outra empresa cadastrada
    E que existe um usuário de acesso "joao@beta.com.br" para a outra empresa
    Quando eu redefinir a senha do acesso da outra empresa pela empresa cadastrada
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Usuário não encontrado"
