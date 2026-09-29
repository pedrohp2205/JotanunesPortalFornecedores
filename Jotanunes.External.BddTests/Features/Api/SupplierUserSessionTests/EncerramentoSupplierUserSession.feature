# language: pt-BR
Funcionalidade: SupplierUserSession - Encerramento

Como fornecedor autenticado
Quero sair do portal
Para que a minha sessão não possa mais ser renovada

Cenário: Encerrar a sessão atual
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que eu estou autenticado com senha definitiva
    Quando eu encerrar a minha sessão
    Então eu recebo uma resposta 204 No Content
    E o refresh token da sessão encerrada não deve mais renovar a sessão
