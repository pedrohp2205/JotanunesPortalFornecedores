# language: pt-BR
Funcionalidade: SupplierUser - Redefinição de senha

Como fornecedor que recebeu o link de redefinição
Quero definir uma nova senha
Para voltar a acessar o portal

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que existe um fornecedor "maria@alfa.com.br" com senha definitiva para a empresa cadastrada
    E que o fornecedor cadastrado solicitou a redefinição de senha e recebeu o token "TOKEN-REDEFINICAO"

Cenário: Redefinir a senha com o token recebido
    Quando eu redefinir a senha de "maria@alfa.com.br" com o token "TOKEN-REDEFINICAO" para "NovaSenha@2026"
    Então eu recebo uma resposta 204 No Content
    E eu consigo entrar com o e-mail "maria@alfa.com.br" e a senha "NovaSenha@2026"

Cenário: Não permitir redefinir com um token inválido
    Quando eu redefinir a senha de "maria@alfa.com.br" com o token "TOKEN-ERRADO" para "NovaSenha@2026"
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Link de redefinição inválido ou expirado. Solicite um novo."

Cenário: Não permitir reutilizar um token já usado
    Dado que a senha de "maria@alfa.com.br" já foi redefinida com o token "TOKEN-REDEFINICAO"
    Quando eu redefinir a senha de "maria@alfa.com.br" com o token "TOKEN-REDEFINICAO" para "OutraSenha@2026"
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Link de redefinição inválido ou expirado. Solicite um novo."

Cenário: Não permitir nova senha curta
    Quando eu redefinir a senha de "maria@alfa.com.br" com o token "TOKEN-REDEFINICAO" para "curta"
    Então eu recebo uma resposta 400 Bad Request
