# language: pt-BR
Funcionalidade: Auth - Login do fornecedor

Como fornecedor com acesso ao Portal do Fornecedor
Quero entrar com meu e-mail e senha
Para acessar as solicitações da minha empresa

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que existe um fornecedor "maria@alfa.com.br" com senha definitiva para a empresa cadastrada

Cenário: Entrar com credenciais válidas
    Quando eu entrar com o e-mail "maria@alfa.com.br" e a senha correta
    Então eu recebo uma resposta 200 OK
    E eu recebo um par de tokens vinculado à empresa cadastrada

Cenário: Entrar com senha incorreta
    Quando eu entrar com o e-mail "maria@alfa.com.br" e a senha "senha-errada"
    Então eu recebo uma resposta 401 Unauthorized
    E eu recebo uma resposta de erro com a mensagem "E-mail ou senha inválidos."

Cenário: Entrar com e-mail inexistente recebe a mesma mensagem de senha incorreta
    Quando eu entrar com o e-mail "ninguem@alfa.com.br" e a senha "senha-errada"
    Então eu recebo uma resposta 401 Unauthorized
    E eu recebo uma resposta de erro com a mensagem "E-mail ou senha inválidos."

Cenário: Consultar os próprios dados após o login
    Dado que eu estou autenticado como o fornecedor "maria@alfa.com.br"
    Quando eu solicitar os dados do usuário autenticado
    Então eu recebo uma resposta 200 OK
    E os dados devem refletir o fornecedor autenticado
