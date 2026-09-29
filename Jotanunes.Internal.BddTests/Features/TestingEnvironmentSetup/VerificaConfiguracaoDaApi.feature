# language: pt-BR
Funcionalidade: Verifica configuração da API interna
Verifica configuração da API interna (back-office)
para garantir que o ambiente de teste esteja corretamente configurado.

Cenário: Verifica acesso anônimo à API
    Quando eu consultar a saúde da API
    Então eu recebo uma resposta 200 OK
