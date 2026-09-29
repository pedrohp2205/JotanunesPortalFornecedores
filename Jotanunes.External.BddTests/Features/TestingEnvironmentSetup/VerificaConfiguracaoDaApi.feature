# language: pt-BR
Funcionalidade: Verifica configuração da API externa
Verifica configuração da API externa (Portal do Fornecedor)
para garantir que o ambiente de teste esteja corretamente configurado.

Cenário: Verifica acesso anônimo à API
    Quando eu consultar a saúde da API
    Então eu recebo uma resposta 200 OK

Cenário: Verifica acesso não autenticado a recurso protegido
    Dado que eu não estou autenticado
    Quando eu solicitar os dados do usuário autenticado
    Então eu recebo uma resposta 401 Unauthorized
