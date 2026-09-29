# language: pt-BR
Funcionalidade: Verifica configuração da base de dados
Verifica que a base de dados de teste está configurada
e que o cenário roda dentro de uma transação.

Cenário: Verifica conexão com o banco de teste
    Quando eu solicitar uma transação à base de dados
    Então a transação deve ser obtida com sucesso
