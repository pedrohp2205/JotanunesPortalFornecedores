# language: pt-BR
Funcionalidade: SupplierUser - Esqueci minha senha

Como fornecedor que esqueceu a senha
Quero pedir um link de redefinição por e-mail
Para voltar a acessar o portal

Cenário: Solicitar a redefinição para um e-mail cadastrado
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que existe um fornecedor "maria@alfa.com.br" com senha definitiva para a empresa cadastrada
    Quando eu solicitar a redefinição de senha para o e-mail "maria@alfa.com.br"
    Então eu recebo uma resposta 204 No Content
    E o fornecedor cadastrado deve ter um token de redefinição de senha válido

Cenário: Responder da mesma forma para um e-mail não cadastrado
    Quando eu solicitar a redefinição de senha para o e-mail "ninguem@alfa.com.br"
    Então eu recebo uma resposta 204 No Content
