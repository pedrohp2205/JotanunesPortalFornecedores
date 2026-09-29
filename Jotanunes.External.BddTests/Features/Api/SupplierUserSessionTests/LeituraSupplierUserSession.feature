# language: pt-BR
Funcionalidade: SupplierUserSession - Leitura

Como fornecedor autenticado
Quero consultar os dados do meu acesso
Para que o portal exiba quem está conectado e a empresa representada

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que existe um fornecedor "maria@alfa.com.br" com senha definitiva para a empresa cadastrada

Cenário: Consultar os próprios dados após o login
    Dado que eu estou autenticado como o fornecedor "maria@alfa.com.br"
    Quando eu solicitar os dados do usuário autenticado
    Então eu recebo uma resposta 200 OK
    E os dados devem refletir o fornecedor autenticado
