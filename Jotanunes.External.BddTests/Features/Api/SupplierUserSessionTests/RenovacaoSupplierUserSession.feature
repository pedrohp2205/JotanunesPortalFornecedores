# language: pt-BR
Funcionalidade: SupplierUserSession - Renovação

Como fornecedor autenticado
Quero renovar meu token de acesso com o refresh token
Para continuar usando o portal sem entrar novamente

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que eu estou autenticado com senha definitiva

Cenário: Renovar a sessão com o refresh token
    Quando eu renovar a sessão com o refresh token atual
    Então eu recebo uma resposta 200 OK
    E eu recebo um novo par de tokens com o refresh token rotacionado

Cenário: Não permitir renovar com um refresh token desconhecido
    Quando eu renovar a sessão com o refresh token "token-desconhecido"
    Então eu recebo uma resposta 401 Unauthorized
    E eu recebo uma resposta de erro com a mensagem "Sessão inválida ou expirada. Faça login novamente."

Cenário: Não permitir renovar uma sessão encerrada
    Dado que eu encerrei a minha sessão
    Quando eu renovar a sessão com o refresh token atual
    Então eu recebo uma resposta 401 Unauthorized
    E eu recebo uma resposta de erro com a mensagem "Sessão inválida ou expirada. Faça login novamente."
